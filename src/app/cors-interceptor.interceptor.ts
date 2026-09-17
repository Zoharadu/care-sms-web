import { Injectable } from '@angular/core';
import {
  HttpErrorResponse,
  HttpRequest,
  HttpHandler,
  HttpEvent,
  HttpInterceptor
} from '@angular/common/http';
import { Observable, catchError, throwError } from 'rxjs';

import { environment } from '../environments/environment';
import { AuthenticationFeedbackService } from './authentication-feedback.service';

function toAbsoluteUrl(url: string): URL | null {
  try {
    const browserBaseUrl = typeof document === 'undefined'
      ? 'http://localhost/'
      : document.baseURI;
    return new URL(url, browserBaseUrl);
  } catch {
    return null;
  }
}

function matchesConfiguredBaseUrl(requestUrl: string, configuredBaseUrl: string): boolean {
  if (!configuredBaseUrl) {
    return false;
  }

  const request = toAbsoluteUrl(requestUrl);
  const base = toAbsoluteUrl(configuredBaseUrl);
  if (!request || !base || request.origin !== base.origin) {
    return false;
  }

  const basePath = base.pathname.replace(/\/+$/, '');
  return request.pathname === basePath || request.pathname.startsWith(`${basePath}/`);
}

export function isBackendRequest(requestUrl: string): boolean {
  return matchesConfiguredBaseUrl(requestUrl, environment.baseUrl);
}

@Injectable()
export class BackendAuthenticationInterceptor implements HttpInterceptor {

  constructor(private readonly authenticationFeedback: AuthenticationFeedbackService) { }

  intercept(request: HttpRequest<unknown>, next: HttpHandler): Observable<HttpEvent<unknown>> {
    if (!isBackendRequest(request.url)) {
      return next.handle(request);
    }

    const authenticatedRequest = request.clone({ withCredentials: true });

    return next.handle(authenticatedRequest).pipe(
      catchError((error: unknown) => {
        if (error instanceof HttpErrorResponse) {
          this.authenticationFeedback.reportHttpStatus(error.status);
        }

        return throwError(() => error);
      }),
    );
  }
}

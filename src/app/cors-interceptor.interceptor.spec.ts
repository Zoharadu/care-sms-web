import {
  HttpErrorResponse,
  HttpHandler,
  HttpRequest,
  HttpResponse,
} from '@angular/common/http';
import { of, throwError } from 'rxjs';

import { environment } from '../environments/environment';
import { AuthenticationFeedbackService } from './authentication-feedback.service';
import { BackendAuthenticationInterceptor } from './cors-interceptor.interceptor';

describe('BackendAuthenticationInterceptor', () => {
  let originalBaseUrl: string;
  let feedback: AuthenticationFeedbackService;
  let interceptor: BackendAuthenticationInterceptor;

  beforeEach(() => {
    originalBaseUrl = environment.baseUrl;
    environment.baseUrl = 'http://localhost:57978/api/';
    feedback = new AuthenticationFeedbackService();
    interceptor = new BackendAuthenticationInterceptor(feedback);
  });

  afterEach(() => {
    environment.baseUrl = originalBaseUrl;
  });

  it('adds credentials to a request under the configured API URL', () => {
    const forwarded = forward(
      new HttpRequest('POST', 'http://localhost:57978/api/SmsTemplates', {}),
    );

    expect(forwarded.withCredentials).toBeTrue();
    expect(forwarded.headers.has('Authorization')).toBeFalse();
  });

  it('adds credentials to the user endpoint under the same API base URL', () => {
    const forwarded = forward(new HttpRequest('GET', 'http://localhost:57978/api/user'));

    expect(forwarded.withCredentials).toBeTrue();
  });

  it('does not change a request to an external service', () => {
    const original = new HttpRequest('GET', 'https://example.com/api/data');
    const forwarded = forward(original);

    expect(forwarded).toBe(original);
    expect(forwarded.withCredentials).toBeFalse();
  });

  it('reports a 401 without retrying or replacing the original error', () => {
    const error = new HttpErrorResponse({ status: 401, statusText: 'Unauthorized' });
    const handler = jasmine.createSpyObj<HttpHandler>('HttpHandler', ['handle']);
    handler.handle.and.returnValue(throwError(() => error));
    let receivedError: unknown;

    interceptor.intercept(
      new HttpRequest('PUT', 'http://localhost:57978/api/SmsRules/1', {}),
      handler,
    ).subscribe({
      error: (caughtError) => receivedError = caughtError,
    });

    expect(handler.handle).toHaveBeenCalledTimes(1);
    expect(receivedError).toBe(error);
    expect(feedback.message()).toContain('לא ניתן לזהות את המשתמש המחובר');
  });

  it('reports a 403 separately from an authentication failure', () => {
    const error = new HttpErrorResponse({ status: 403, statusText: 'Forbidden' });
    const handler = jasmine.createSpyObj<HttpHandler>('HttpHandler', ['handle']);
    handler.handle.and.returnValue(throwError(() => error));

    interceptor.intercept(
      new HttpRequest('POST', 'http://localhost:57978/api/SmsTemplates', {}),
      handler,
    ).subscribe({
      error: () => undefined,
    });

    expect(feedback.message()).toContain('אין למשתמש המחובר הרשאה');
  });

  function forward(request: HttpRequest<unknown>): HttpRequest<unknown> {
    let forwardedRequest: HttpRequest<unknown> | undefined;
    const handler = {
      handle: (nextRequest: HttpRequest<unknown>) => {
        forwardedRequest = nextRequest;
        return of(new HttpResponse());
      },
    } as HttpHandler;

    interceptor.intercept(request, handler).subscribe();

    if (!forwardedRequest) {
      throw new Error('The request was not forwarded.');
    }

    return forwardedRequest;
  }
});

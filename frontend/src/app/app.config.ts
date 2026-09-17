import { ApplicationConfig, provideZoneChangeDetection } from '@angular/core';
import {
  HTTP_INTERCEPTORS,
  provideHttpClient,
  withInterceptorsFromDi,
} from '@angular/common/http';
import { provideRouter, withHashLocation } from '@angular/router';
import { routes } from './app.routes';
import { BackendAuthenticationInterceptor } from './cors-interceptor.interceptor';

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideHttpClient(withInterceptorsFromDi()),
    {
      provide: HTTP_INTERCEPTORS,
      useClass: BackendAuthenticationInterceptor,
      multi: true,
    },
    provideRouter(routes, withHashLocation())
  ]
};

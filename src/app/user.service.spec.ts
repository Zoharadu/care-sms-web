import { HttpClient } from '@angular/common/http';
import { of, Subject, throwError } from 'rxjs';

import { environment } from '../environments/environment';
import { ApiService } from './api.service';
import { User, UserService } from './user.service';

describe('UserService Windows Authentication request', () => {
  let httpClient: HttpClient;
  let httpGet: jasmine.Spy;
  let originalBaseUrl: string;

  beforeEach(() => {
    originalBaseUrl = environment.baseUrl;
    environment.baseUrl = 'http://localhost:57978/api/';
    httpGet = jasmine.createSpy('HttpClient.get');
    httpClient = { get: httpGet } as unknown as HttpClient;
  });

  afterEach(() => {
    environment.baseUrl = originalBaseUrl;
  });

  it('builds the user endpoint from the configured API base URL', () => {
    const response = new Subject<User>();
    httpGet.and.returnValue(response.asObservable());

    const service = new UserService(httpClient);

    expect(httpGet).toHaveBeenCalledOnceWith(
      'http://localhost:57978/api/user',
    );

    response.next({ id: '1', fullName: 'Test User', email: 'test@example.com' });
    response.complete();
    expect(service.currentUser?.fullName).toBe('Test User');
  });

  it('does not construct authentication options in the service', () => {
    httpGet.and.returnValue(of({ id: '1', fullName: 'Test User', email: 'test@example.com' }));

    new UserService(httpClient);

    const [, options] = httpGet.calls.mostRecent().args;
    expect(options).toBeUndefined();
  });

  it('supports a relative API base URL', () => {
    environment.baseUrl = '/smswebapi/api/';
    httpGet.and.returnValue(of({ id: '1', fullName: 'Test User', email: 'test@example.com' }));

    new UserService(httpClient);

    expect(httpGet).toHaveBeenCalledOnceWith('/smswebapi/api/user');
  });

  it('does not retry indefinitely after a 401 response', () => {
    httpGet.and.returnValue(throwError(() => ({ status: 401 })));

    const service = new UserService(httpClient);
    service.loadUser();

    expect(httpGet).toHaveBeenCalledTimes(1);
    expect(service.currentUser).toBeNull();
  });
});

describe('HTTP behavior outside the user request', () => {
  it('uses the configured API base URL for other API calls', () => {
    const originalBaseUrl = environment.baseUrl;
    environment.baseUrl = '/api/';
    const httpGet = jasmine.createSpy('HttpClient.get').and.returnValue(of([]));
    const apiService = new ApiService({ get: httpGet } as unknown as HttpClient);

    apiService.getProjects().subscribe();

    expect(httpGet).toHaveBeenCalledOnceWith('/api/SmsProjects');
    environment.baseUrl = originalBaseUrl;
  });
});

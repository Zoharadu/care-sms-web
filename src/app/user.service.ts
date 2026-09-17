import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable } from 'rxjs';

import { environment } from '../environments/environment';

export interface User {
  id: string;
  fullName: string;
  email: string;
}

@Injectable({
  providedIn: 'root'
})
export class UserService {

  private userSubject = new BehaviorSubject<User | null>(null);
  private userLoadStarted = false;

  readonly user$ = this.userSubject.asObservable();

  constructor(private http: HttpClient) {
    this.loadUser();
  }

  getCurrentUser(): Observable<User> {
    const apiBaseUrl = environment.baseUrl.replace(/\/+$/, '');
    return this.http.get<User>(`${apiBaseUrl}/user`);
  }

  loadUser(): void {
    if (this.userSubject.value || this.userLoadStarted) {
      return;
    }

    this.userLoadStarted = true;
    this.getCurrentUser().subscribe({
      next: (user) => this.userSubject.next(user),
      error: () => this.userSubject.next(null),
    });
  }

  get currentUser(): User | null {
    return this.userSubject.value;
  }
}

import { Injectable, signal } from '@angular/core';

const UNAUTHENTICATED_MESSAGE =
  'לא ניתן לזהות את המשתמש המחובר. יש לוודא שהאתר פתוח באמצעות משתמש Windows ארגוני.';
const FORBIDDEN_MESSAGE =
  'אין למשתמש המחובר הרשאה לבצע פעולה זו. ניתן לפנות למנהל המערכת לקבלת הרשאה.';

export function isAuthenticationError(error: unknown): boolean {
  if (typeof error !== 'object' || error === null || !('status' in error)) {
    return false;
  }

  const status = Number(error.status);
  return status === 401 || status === 403;
}

@Injectable({
  providedIn: 'root',
})
export class AuthenticationFeedbackService {
  readonly message = signal<string | null>(null);

  reportHttpStatus(status: number): void {
    if (status === 401) {
      this.message.set(UNAUTHENTICATED_MESSAGE);
    } else if (status === 403) {
      this.message.set(FORBIDDEN_MESSAGE);
    }
  }

  clear(): void {
    this.message.set(null);
  }
}

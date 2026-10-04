import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { catchError, throwError } from 'rxjs';

/**
 * Interceptor גלובלי לטיפול בשגיאות HTTP.
 * 409 (Conflict) מטופל ברכיב עצמו (רענון), כאן רק מציגים הודעה כללית לשאר השגיאות.
 */
export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const snackBar = inject(MatSnackBar);

  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      // 409 מטופל נקודתית ברכיב (הצגת dialog רענון) - לא מציגים כאן snackbar כפול
      if (error.status !== 409) {
        const message = error.error?.detail || error.error?.title || 'אירעה שגיאה. נסה שוב.';
        snackBar.open(message, 'סגור', { duration: 5000, direction: 'rtl' });
      }
      return throwError(() => error);
    })
  );
};

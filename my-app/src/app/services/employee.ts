import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { environment } from '../../environments/environment';

export interface Employee {
  id: number;
  name: string;
  position: string;
  email: string;
  photoUrl?: string | null;
  showInDirectory?: boolean;
}

@Injectable({ providedIn: 'root' })
export class EmployeeService {
  private http = inject(HttpClient);
  private url = `${environment.apiUrl}/employees`;
  private apiOrigin = environment.apiUrl.replace(/\/api\/?$/, '');
  private loadingId: number | null = null;

  readonly current = signal<Employee | null>(null);

  getAll(): Observable<Employee[]> {
    return this.http.get<Employee[]>(this.url);
  }

  getById(id: number): Observable<Employee> {
    return this.http.get<Employee>(`${this.url}/${id}`);
  }

  loadCurrent(id: number): void {
    if (this.current()?.id === id || this.loadingId === id) {
      return;
    }

    this.loadingId = id;
    this.getById(id).subscribe({
      next: employee => {
        this.current.set(employee);
        this.loadingId = null;
      },
      error: err => {
        this.loadingId = null;
        console.error('API error:', err);
      }
    });
  }

  uploadPhoto(id: number, file: File): Observable<Employee> {
    const form = new FormData();
    form.append('file', file);
    return this.http.post<Employee>(`${this.url}/${id}/photo`, form).pipe(tap(e => this.updateCurrent(e)));
  }

  deletePhoto(id: number): Observable<Employee> {
    return this.http.delete<Employee>(`${this.url}/${id}/photo`).pipe(tap(e => this.updateCurrent(e)));
  }

  setPhotoVisibility(id: number, showInDirectory: boolean): Observable<Employee> {
    return this.http
      .put<Employee>(`${this.url}/${id}/photo/visibility`, { showInDirectory })
      .pipe(tap(e => this.updateCurrent(e)));
  }

  photoSrc(employee: Employee | null): string | null {
    const path = employee?.photoUrl;
    if (!path) {
      return null;
    }
    return path.startsWith('http') ? path : `${this.apiOrigin}${path}`;
  }

  private updateCurrent(employee: Employee): void {
    if (this.current()?.id === employee.id) {
      this.current.set(employee);
    }
  }
}

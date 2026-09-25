import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export type BookingStatus = 'Pending' | 'Approved' | 'Rejected';

export interface MeetingRoom {
  id: number;
  name: string;
  location: string;
  capacity: number;
  equipment: string[];
  requiresApproval: boolean;
}

export interface Booking {
  id: number;
  roomId: number;
  title: string;
  date: string;       // yyyy-MM-dd
  startTime: string;  // HH:mm:ss
  endTime: string;    // HH:mm:ss
  organizerId: number;
  participantIds: number[];
  status: BookingStatus;
  createdAt: string;
}

export interface CreateBookingRequest {
  roomId: number;
  title: string;
  date: string;
  startTime: string;
  endTime: string;
  organizerId: number;
  participantIds: number[];
}

export interface BookingQuery {
  from?: string;
  to?: string;
  roomId?: number;
  status?: BookingStatus;
}

@Injectable({ providedIn: 'root' })
export class MeetingService {
  private http = inject(HttpClient);
  private roomsUrl = `${environment.apiUrl}/meeting-rooms`;
  private bookingsUrl = `${environment.apiUrl}/bookings`;

  getRooms(): Observable<MeetingRoom[]> {
    return this.http.get<MeetingRoom[]>(this.roomsUrl);
  }

  getBookings(query: BookingQuery = {}): Observable<Booking[]> {
    let params = new HttpParams();
    for (const [key, value] of Object.entries(query)) {
      if (value !== undefined && value !== null && value !== '') {
        params = params.set(key, String(value));
      }
    }
    return this.http.get<Booking[]>(this.bookingsUrl, { params });
  }

  getEmployeeBookings(employeeId: number, from?: string): Observable<Booking[]> {
    const params = from ? new HttpParams().set('from', from) : undefined;
    return this.http.get<Booking[]>(`${this.bookingsUrl}/employee/${employeeId}`, { params });
  }

  create(request: CreateBookingRequest): Observable<Booking> {
    return this.http.post<Booking>(this.bookingsUrl, request);
  }

  approve(id: number): Observable<Booking> {
    return this.http.post<Booking>(`${this.bookingsUrl}/${id}/approve`, {});
  }

  reject(id: number): Observable<Booking> {
    return this.http.post<Booking>(`${this.bookingsUrl}/${id}/reject`, {});
  }

  cancel(id: number): Observable<void> {
    return this.http.delete<void>(`${this.bookingsUrl}/${id}`);
  }
}

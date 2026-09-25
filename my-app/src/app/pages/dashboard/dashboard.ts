import { Component, inject, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import {forkJoin} from 'rxjs';
import { EmployeeService, Employee} from '../../services/employee';
import { Booking, MeetingRoom, MeetingService} from '../../services/meeting';
import { toIsoDate } from '../../shared/utils/date';




interface RequestItem {
  title: string;
  submittedOn: string;
}

interface StatTile {
  label: string;
  icon: string;
  used: number;
  total: number;
}

@Component({
  selector: 'app-dashboard',
  imports: [RouterLink],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.css'
})
export class Dashboard implements OnInit {
  private employeeService = inject(EmployeeService);
  private meetingService = inject(MeetingService);

  private readonly currrentUserId = 1;

  todayMeetings = signal<Booking[]>([]);
  rooms = signal<MeetingRoom[]>([]);

  private today = new Date();

  private georgianWeekdays = [
    'კვირა', 'ორშაბათი', 'სამშაბათი', 'ოთხშაბათი', 'ხუთშაბათი', 'პარასკევი', 'შაბათი',
  ];

  private georgianMonths = [
    'იანვარი', 'თებერვალი', 'მარტი', 'აპრილი', 'მაისი', 'ივნისი',
    'ივლისი', 'აგვისტო', 'სექტემბერი', 'ოქტომბერი', 'ნოემბერი', 'დეკემბერი',
  ];

  get formattedDate(): string {
    const weekday = this.georgianWeekdays[this.today.getDay()];
    const month = this.georgianMonths[this.today.getMonth()];
    return `${weekday}, ${this.today.getDate()} ${month} ${this.today.getFullYear()}`;
  }

  get formattedTime(): string {
    return this.today.toLocaleTimeString('ka-GE', { hour: '2-digit', minute: '2-digit' });
  }

  employee = signal<Employee | null>(null);

  ngOnInit() {
    this.employeeService.getById(1).subscribe({
      next: data => this.employee.set(data),
      error: err => console.error('API error:', err)
    });
    this.loadTodayMeetings();
  }

  private loadTodayMeetings() {
    const today = toIsoDate(new Date());

    forkJoin({
      rooms: this.meetingService.getRooms(),
      bookings: this.meetingService.getEmployeeBookings(this.currrentUserId, today)
    }).subscribe({
      next: ({ rooms, bookings }) => {
        this.rooms.set(rooms);
        this.todayMeetings.set(
          bookings.filter(b => b.date === today && b.status !== 'Rejected')
        );
      },
      error: err => console.error('API error:', err)
    });

  }


  leaveDaysUsed = 0;
  leaveDaysTotal = 24;

  dayOffUsed = 0;
  dayOffTotal = 6;



  requests: RequestItem[] = [
    { title: 'დისტანციური მუშაობის მოთხოვნა', submittedOn: '18 სექტემბერს' },
  ];

  statTiles: StatTile[] = [
    { label: 'ანაზღაურებადი შვებულება', icon: '🌴', used: 0, total: 24 },
    { label: 'არაანაზღაურებადი შვებულება', icon: '📄', used: 0, total: 15 },
    { label: 'დასვენების დღეები', icon: '📅', used: 0, total: 6 },
  ];

  ringGradient(used: number, total: number, color: string): string {
    const remaining = total - used;
    const pct = total === 0 ? 0 : Math.round((remaining / total) * 100);
    return `conic-gradient(${color} 0% ${pct}%, var(--color-border) ${pct}% 100%)`;
  }

  progressPct(used: number, total: number): number {
    const remaining = total - used;
    return total === 0 ? 0 : Math.round((remaining / total) * 100);
  }

  firstName(fullName: string): string {
    return fullName.split(' ')[0];
  }

  roomName(id: number): string {
    return this.rooms().find(r => r.id === id)?.name ?? '-';
  }

  time (t: string): string {
    return t.slice(0,5);
  }
}

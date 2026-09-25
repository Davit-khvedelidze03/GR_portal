import { Component, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { forkJoin } from 'rxjs';
import { Booking, BookingStatus, MeetingRoom, MeetingService } from '../../services/meeting';
import { Employee, EmployeeService } from '../../services/employee';
import { Icon } from '../../shared/icon/icon';
import { toIsoDate } from '../../shared/utils/date';

type Tab = 'rooms' | 'approval';

interface BookingForm {
  roomId: number | null;
  title: string;
  date: string;
  startTime: string;
  endTime: string;
  participantIds: number[];
}

const WEEKDAYS_SHORT = ['კვ', 'ორშ', 'სამ', 'ოთხ', 'ხუთ', 'პარ', 'შაბ'];
const MONTHS = [
  'იანვარი', 'თებერვალი', 'მარტი', 'აპრილი', 'მაისი', 'ივნისი',
  'ივლისი', 'აგვისტო', 'სექტემბერი', 'ოქტომბერი', 'ნოემბერი', 'დეკემბერი',
];

function addDays(d: Date, days: number): Date {
  const copy = new Date(d);
  copy.setDate(copy.getDate() + days);
  return copy;
}

@Component({
  selector: 'app-meeting-rooms',
  imports: [FormsModule, RouterLink, Icon],
  templateUrl: './meeting-rooms.html',
  styleUrl: './meeting-rooms.css',
})
export class MeetingRooms implements OnInit {
  private meetingService = inject(MeetingService);
  private employeeService = inject(EmployeeService);
  private destroyRef = inject(DestroyRef);

  readonly currentUserId = 1;

  activeTab = signal<Tab>('rooms');
  loading = signal(true);
  loadError = signal<string | null>(null);

  rooms = signal<MeetingRoom[]>([]);
  employees = signal<Employee[]>([]);
  todayBookings = signal<Booking[]>([]);
  pendingBookings = signal<Booking[]>([]);
  myBookings = signal<Booking[]>([]);

  now = signal(new Date());


  modalOpen = signal(false);
  saving = signal(false);
  formError = signal<string | null>(null);
  form: BookingForm = this.emptyForm();

  toast = signal<string | null>(null);
  private toastTimer?: ReturnType<typeof setTimeout>;

  readonly todayIso = toIsoDate(new Date());

  selectedRoom(): MeetingRoom | null {
    return this.rooms().find(r => r.id === this.form.roomId) ?? null;
  }

  ngOnInit() {
    this.loadAll();
    const timer = setInterval(() => this.now.set(new Date()), 60_000);
    this.destroyRef.onDestroy(() => {
      clearInterval(timer);
      clearTimeout(this.toastTimer);
    });
  }

  loadAll() {
    this.loading.set(true);
    this.loadError.set(null);

    forkJoin({
      rooms: this.meetingService.getRooms(),
      employees: this.employeeService.getAll(),
    }).subscribe({
      next: ({ rooms, employees }) => {
        this.rooms.set(rooms);
        this.employees.set(employees);
        this.refreshBookings();
      },
      error: err => {
        console.error('API error:', err);
        this.loadError.set('მონაცემების ჩატვირთვა ვერ მოხერხდა');
        this.loading.set(false);
      },
    });
  }

  refreshBookings() {
    forkJoin({
      today: this.meetingService.getBookings({ from: this.todayIso, to: this.todayIso }),
      pending: this.meetingService.getBookings({ status: 'Pending' }),
      mine: this.meetingService.getEmployeeBookings(this.currentUserId, this.todayIso),
    }).subscribe({
      next: ({ today, pending, mine }) => {
        this.todayBookings.set(today);
        this.pendingBookings.set(pending);
        this.myBookings.set(mine.filter(b => b.status !== 'Rejected'));
        this.loading.set(false);
      },
      error: err => {
        console.error('API error:', err);
        this.loadError.set('ჯავშნების ჩატვირთვა ვერ მოხერხდა');
        this.loading.set(false);
      },
    });
  }

  isRoomBusy(room: MeetingRoom): boolean {
    const nowTime = this.timeOf(this.now());
    return this.todayBookings().some(b =>
      b.roomId === room.id && b.status === 'Approved' &&
      b.startTime <= nowTime && b.endTime > nowTime);
  }

  nextBookingToday(room: MeetingRoom): Booking | null {
    const nowTime = this.timeOf(this.now());
    return this.todayBookings().find(b =>
      b.roomId === room.id && b.status !== 'Rejected' && b.endTime > nowTime) ?? null;
  }



  approve(b: Booking) {
    this.meetingService.approve(b.id).subscribe({
      next: () => { this.showToast('ჯავშანი დადასტურდა'); this.refreshBookings(); },
      error: err => this.showToast(this.errorMessage(err)),
    });
  }

  reject(b: Booking) {
    this.meetingService.reject(b.id).subscribe({
      next: () => { this.showToast('ჯავშანი უარყოფილია'); this.refreshBookings(); },
      error: err => this.showToast(this.errorMessage(err)),
    });
  }

  cancel(b: Booking) {
    if (!confirm(`გავაუქმოთ "${b.title}"?`)) return;
    this.meetingService.cancel(b.id).subscribe({
      next: () => { this.showToast('შეხვედრა გაუქმდა'); this.refreshBookings(); },
      error: err => this.showToast(this.errorMessage(err)),
    });
  }


  openBooking(room?: MeetingRoom) {
    this.form = this.emptyForm();
    if (room) this.form.roomId = room.id;
    this.formError.set(null);
    this.modalOpen.set(true);
  }

  closeModal() {
    if (this.saving()) return;
    this.modalOpen.set(false);
  }

  toggleParticipant(id: number) {
    const ids = this.form.participantIds;
    this.form.participantIds = ids.includes(id) ? ids.filter(x => x !== id) : [...ids, id];
  }

  submitBooking() {
    const f = this.form;
    if (!f.roomId || !f.title.trim() || !f.date || !f.startTime || !f.endTime) {
      this.formError.set('შეავსეთ ყველა სავალდებულო ველი');
      return;
    }
    if (f.endTime <= f.startTime) {
      this.formError.set('დასრულების დრო უნდა იყოს დაწყების დროზე გვიან');
      return;
    }

    const room = this.rooms().find(r => r.id === f.roomId);
    this.saving.set(true);
    this.formError.set(null);

    this.meetingService.create({
      roomId: f.roomId,
      title: f.title.trim(),
      date: f.date,
      startTime: `${f.startTime}:00`,
      endTime: `${f.endTime}:00`,
      organizerId: this.currentUserId,
      participantIds: f.participantIds,
    }).subscribe({
      next: created => {
        this.saving.set(false);
        this.modalOpen.set(false);
        this.showToast(created.status === 'Pending'
          ? `"${room?.name}" — ჯავშანი გაიგზავნა დასადასტურებლად`
          : `"${room?.name}" დაიჯავშნა`);
        this.refreshBookings();
      },
      error: err => {
        this.saving.set(false);
        this.formError.set(this.errorMessage(err));
      },
    });
  }


  roomName(id: number): string {
    return this.rooms().find(r => r.id === id)?.name ?? '—';
  }

  employeeName(id: number): string {
    return this.employees().find(e => e.id === id)?.name ?? `#${id}`;
  }

  otherEmployees(): Employee[] {
    return this.employees().filter(e => e.id !== this.currentUserId);
  }

  isOrganizer(b: Booking): boolean {
    return b.organizerId === this.currentUserId;
  }

  time(t: string): string {
    return t.slice(0, 5);
  }

  formatDate(iso: string): string {
    if (iso === this.todayIso) return 'დღეს';
    if (iso === toIsoDate(addDays(new Date(), 1))) return 'ხვალ';
    const [y, m, d] = iso.split('-').map(Number);
    const date = new Date(y, m - 1, d);
    return `${WEEKDAYS_SHORT[date.getDay()]}, ${d} ${MONTHS[m - 1]}`;
  }

  statusLabel(s: BookingStatus): string {
    return { Pending: 'მოლოდინში', Approved: 'დადასტურებული', Rejected: 'უარყოფილი' }[s];
  }

  private timeOf(d: Date): string {
    return `${String(d.getHours()).padStart(2, '0')}:${String(d.getMinutes()).padStart(2, '0')}:00`;
  }

  private emptyForm(): BookingForm {
    const next = new Date();
    next.setMinutes(0, 0, 0);
    next.setHours(next.getHours() + 1);
    const end = new Date(next);
    end.setHours(end.getHours() + 1);
    const hhmm = (d: Date) => this.timeOf(d).slice(0, 5);

    return {
      roomId: null,
      title: '',
      date: toIsoDate(next),
      startTime: hhmm(next),
      endTime: end.getDate() === next.getDate() ? hhmm(end) : '23:59',
      participantIds: [],
    };
  }

  private errorMessage(err: unknown): string {
    if (err instanceof HttpErrorResponse) {
      if (err.error?.message) return err.error.message;
      if (err.status === 0) return 'სერვერთან კავშირი ვერ დამყარდა';
      if (err.error?.errors) return 'შეამოწმეთ შეყვანილი მონაცემები';
    }
    return 'მოხდა შეცდომა, სცადეთ თავიდან';
  }

  private showToast(message: string) {
    this.toast.set(message);
    clearTimeout(this.toastTimer);
    this.toastTimer = setTimeout(() => this.toast.set(null), 3500);
  }
}

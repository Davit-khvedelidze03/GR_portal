import { Component, computed, ElementRef, inject, OnInit, viewChild } from '@angular/core';
import { EmployeeService } from '../../services/employee';
import { RouterLink, RouterLinkActive } from '@angular/router';
import {Icon} from '../../shared/icon/icon';

interface NavLink {
  label: string;
  path?: string;
  hasDropdown?: boolean;
}

@Component({
  selector: 'app-navbar',
  imports: [Icon, RouterLink, RouterLinkActive],
  templateUrl: './navbar.html',
  styleUrl: './navbar.css'
})
export class Navbar implements OnInit {
  private employeeService = inject(EmployeeService)
  navLinks: NavLink[] = [
    { label: 'მთავარი', path: '/' },
    { label: 'შვებულებები', hasDropdown: true },
    { label: 'საშვები', path: '/passes' },
    { label: 'მოთხოვნები' },
    { label: 'ინფორმაცია' },
    { label: 'ადმინისტრირება' },
  ];

  employee = this.employeeService.current;
  photoSrc = computed(() => this.employeeService.photoSrc(this.employee()));

  photoDialog = viewChild<ElementRef<HTMLDialogElement>>('photoDialog');

  openPhoto() {
    this.photoDialog()?.nativeElement.showModal();
  }

  closePhoto() {
    this.photoDialog()?.nativeElement.close();
  }

  onDialogClick(event: MouseEvent) {
    if (event.target === this.photoDialog()?.nativeElement) {
      this.closePhoto();
    }
  }
  ngOnInit() {
    this.employeeService.loadCurrent(1);
  }

  initials (name: string){
    return name
      .split(' ')
      .map(part => part.charAt(0))
      .join('')
      .toUpperCase();
  }
}






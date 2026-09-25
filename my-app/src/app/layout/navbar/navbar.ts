import { Component, inject, OnInit, signal } from '@angular/core';
import {EmployeeService, Employee } from '../../services/employee';

interface NavLink {
  label: string;
  hasDropdown?: boolean;
}

@Component({
  selector: 'app-navbar',
  templateUrl: './navbar.html',
  styleUrl: './navbar.css'
})
export class Navbar implements OnInit {
  private employeeService = inject(EmployeeService)
  navLinks: NavLink[] = [
    { label: 'მთავარი' },
    { label: 'შვებულებები', hasDropdown: true },
    { label: 'მოთხოვნები' },
    { label: 'ინფორმაცია' },
    { label: 'ადმინისტრირება' },
  ];

  employee = signal<Employee | null>(null);

  ngOnInit() {
    this.employeeService.getById(1).subscribe({
      next: data =>this.employee.set(data),
      error: err => console.error('API error:', err)
    })
  }

  initials (name: string){
    return name
      .split(' ')
      .map(part => part.charAt(0))
      .join('')
      .toUpperCase();
  }
}






import { Component, OnInit } from '@angular/core';
import { MotorcycleCardComponent } from '../../components/motorcycle-card/motorcycle-card.component';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { MotorcyclesService } from '../../service/motorcycles.service';
import { Motorcycle } from '../../interfaces/motorcycle.interface';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [CommonModule, MotorcycleCardComponent, RouterModule],
  templateUrl: './home.component.html',
  styleUrl: './home.component.scss'
})
export class HomeComponent implements OnInit {
 motorcycles: Motorcycle[] = [];

  constructor(
    private motorcyclesService: MotorcyclesService
  ) {}

  ngOnInit(): void {
    this.loadMotorcycles();
  }

  loadMotorcycles(): void {
  this.motorcyclesService.getMyMotorcycles().subscribe({
    next: (data) => (this.motorcycles = data),
    error: (err) => console.error(err),
  });
  }
}

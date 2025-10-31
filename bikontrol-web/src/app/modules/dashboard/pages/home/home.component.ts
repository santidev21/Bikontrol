import { Component } from '@angular/core';
import { MotorcycleCardComponent } from '../../components/motorcycle-card/motorcycle-card.component';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [CommonModule, MotorcycleCardComponent],
  templateUrl: './home.component.html',
  styleUrl: './home.component.scss'
})
export class HomeComponent {
 motorcycles = [
    {
      name: 'TTR 200',
      brand: 'Yamaha',
      year: 2020,
      nickname: 'Rayo',
      km: 1200,
      image: '/assets/images/defaults/motorcycle-placeholder.webp'
    },
    {
      name: 'CB 125',
      brand: 'Honda',
      year: 2016,
      nickname: 'Fiera',
      km: 5400,
      image: '/assets/images/defaults/motorcycle-placeholder.webp'
    }
  ];
}

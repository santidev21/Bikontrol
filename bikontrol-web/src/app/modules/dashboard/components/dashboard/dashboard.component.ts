import { Component } from '@angular/core';
import { MotorcycleOverview } from '../../interfaces/motorcycle-overview.model';

@Component({
  selector: 'app-dashboard',
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.scss']
})
export class DashboardComponent {

  motorcycles : MotorcycleOverview[] = [
  {
    id: '1',
    name: 'Voge 300DS',
    plate: 'ABC123',
    brand: 'Voge',
    model: '300DS',
    modelYear: 2023,
    lastMileage: 8600,
    image: 'assets/images/motorcycles/voge-300-ds.png',
  },
  {
    id: '2',
    name: 'TTR 200',
    plate: 'ROD78E',
    brand: 'AKT',
    model: 'TTR 200',
    modelYear: 2020,
    lastMileage: 14200,
    image: 'assets/images/motorcycles/ttr-200.png',
  }
];

}

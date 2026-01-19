import { Component, Input } from '@angular/core';
import { FormGroup, ReactiveFormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-monitoring-type-selector',
  standalone: true,
  imports: [ReactiveFormsModule, CommonModule],
  templateUrl: './monitoring-type-selector.component.html',
  styleUrl: './monitoring-type-selector.component.scss'
})
export class MonitoringTypeSelectorComponent {
  @Input() form!: FormGroup;
  @Input() isEditMode = false;

  hasError(field: string, type: string): boolean {
    if (!this.form) return false;
    
    const control = this.form.get(field);
    const monitoringType = this.form.get('monitoringType')?.value;
    
    // Only validate kmInterval if monitoringType is 'km'
    if (field === 'kmInterval' && monitoringType !== 'km') {
      return false;
    }
    
    // Only validate time-related fields if monitoringType is 'time'
    if ((field === 'timeIntervalWeeks' || field === 'timeIntervalUnit') && monitoringType !== 'time') {
      return false;
    }
    
    return !!control && control.hasError(type) && control.touched;
  }
}

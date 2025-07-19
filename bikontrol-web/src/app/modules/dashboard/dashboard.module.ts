import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { DashboardRoutingModule } from './dashboard-routing.module';
import { DashboardComponent } from './components/dashboard/dashboard.component';
import { OverviewCardComponent } from './components/overview-card/overview-card.component';



@NgModule({
  declarations: [DashboardComponent, OverviewCardComponent],
  imports: [
    CommonModule,
    DashboardRoutingModule    
  ],
  exports: [DashboardComponent, OverviewCardComponent]
})
export class DashboardModule { }

import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { AppComponent } from './app.component';
import { AppRoutingModule } from './app-routing.module';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { HttpClientModule } from '@angular/common/http';
import { LayoutModule } from './modules/layout/layout.module';

@NgModule({
  declarations: [AppComponent],
  imports: [
    BrowserModule, 
    BrowserAnimationsModule, 
    HttpClientModule, 
    AppRoutingModule,
    LayoutModule  
  ],
  providers: [],
  bootstrap: [AppComponent]
})
export class AppModule {}

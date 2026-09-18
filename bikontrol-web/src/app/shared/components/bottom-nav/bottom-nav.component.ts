import { Component, ChangeDetectionStrategy } from '@angular/core';
import { RouterModule } from '@angular/router';

@Component({
    selector: 'app-bottom-nav',
    imports: [RouterModule],
    templateUrl: './bottom-nav.component.html',
    changeDetection: ChangeDetectionStrategy.Eager,
    styleUrl: './bottom-nav.component.scss'
})
export class BottomNavComponent {

}

import {Component, DestroyRef, inject, OnInit} from '@angular/core';
import {Router, RouterModule} from '@angular/router';
import {AppAuthService} from "./app/services/app-auth.service";

@Component({
    selector: 'app-root',
    standalone: true,
    imports: [RouterModule],
    template: `<router-outlet></router-outlet>`
})
export class AppComponent {
    
   #appAuth = inject(AppAuthService);
   #router = inject(Router);
   
   constructor() {
   }
   

}

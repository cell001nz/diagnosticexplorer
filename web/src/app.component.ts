import {Component, DestroyRef, inject, OnInit} from '@angular/core';
import {Router, RouterModule} from '@angular/router';
import {AuthService} from "./app/services/auth.service";

@Component({
    selector: 'app-root',
    standalone: true,
    imports: [RouterModule],
    template: `<router-outlet></router-outlet>`
})
export class AppComponent {
    
   #appAuth = inject(AuthService);
   #router = inject(Router);
   
   constructor() {
   }
   

}

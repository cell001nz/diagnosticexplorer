import {Component, DestroyRef, inject, OnInit} from '@angular/core';
import {Router, RouterModule} from '@angular/router';
import {AppAuthService} from "./app/services/app-auth.service";
import {MsalRedirectComponent} from "@azure/msal-angular";

@Component({
    selector: 'app-root',
    standalone: true,
    imports: [RouterModule],
    template: `<router-outlet></router-outlet>`
})
export class AppComponent implements OnInit {
    
   #appAuth = inject(AppAuthService);
   #router = inject(Router);
   
   constructor() {
   }
   
   async ngOnInit() {
       await this.#appAuth.initialiseAsync();
   }


}

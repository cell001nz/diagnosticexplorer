import {Component, computed, inject, resource} from '@angular/core';
import {RouterModule} from "@angular/router";
import {AuthService} from "@services/auth.service";

@Component({
  selector: 'app-account',
    imports: [
        RouterModule,
    ],
  templateUrl: './account.component.html',
  styleUrl: './account.component.scss'
})
export class AccountComponent {
  
  #authSvc = inject(AuthService);
  
  accountResource = resource({
    loader: () => this.#authSvc.getMyAccount()
  });
  
  
}

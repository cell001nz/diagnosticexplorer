import {Component, computed, inject} from '@angular/core';
import {JsonPipe} from "@angular/common";
import {AuthService} from "@services/auth.service";

@Component({
  selector: 'app-account',
    imports: [
        JsonPipe,
    ],
  templateUrl: './account.component.html',
  styleUrl: './account.component.scss'
})
export class AccountComponent {
  
  #authSvc = inject(AuthService);
  account = computed(() => this.#authSvc.account());  

}

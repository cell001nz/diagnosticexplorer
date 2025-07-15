import {Component, computed, inject} from '@angular/core';
import {AppAuthService} from "../../services/app-auth.service";
import {JsonPipe} from "@angular/common";

@Component({
  selector: 'app-account',
  imports: [
    JsonPipe
  ],
  templateUrl: './account.component.html',
  styleUrl: './account.component.scss'
})
export class AccountComponent {
  
  #authSvc = inject(AppAuthService);

  
  account = computed(() => this.#authSvc.account());
  
}

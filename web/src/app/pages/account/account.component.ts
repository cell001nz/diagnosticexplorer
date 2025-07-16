import {Component, computed, inject} from '@angular/core';
import {AppAuthService} from "../../services/app-auth.service";
import {JsonPipe} from "@angular/common";
import {ShowDataComponent} from "../../show-data/show-data.component";

@Component({
  selector: 'app-account',
    imports: [
        JsonPipe,
        ShowDataComponent
    ],
  templateUrl: './account.component.html',
  styleUrl: './account.component.scss'
})
export class AccountComponent {
  
  #authSvc = inject(AppAuthService);

  
  account = computed(() => this.#authSvc.account());
  
}

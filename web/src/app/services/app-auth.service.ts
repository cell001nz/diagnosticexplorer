import {computed, inject, Injectable, signal} from '@angular/core';
import {filter, takeUntil, tap} from "rxjs/operators";
import {takeUntilDestroyed} from "@angular/core/rxjs-interop";
import {ActivatedRoute, Router} from "@angular/router";
import {HttpClient} from "@angular/common/http";
import {AuthMe} from "../../AuthMe";
import {firstValueFrom} from "rxjs";

@Injectable({
  providedIn: 'root'
})
export class AppAuthService {

    #route = inject(ActivatedRoute);
    #router = inject(Router);
    #http = inject(HttpClient);
    
    account = signal<AuthMe | undefined>(undefined);
    isLoggedIn = computed(() => !!(this.account()?.clientPrincipal));

    constructor() {}

  
  
    login() {
        this.#router.navigate(['/login']);
    }
  
    logout() {
        window.location.assign('/.auth/logout');
    }

    async initialiseAsync() {
        let auth = await firstValueFrom(this.#http.get<AuthMe>('/.auth/me'));
        this.account.set(auth);
    }
}

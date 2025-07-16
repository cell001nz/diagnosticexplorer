import {computed, inject, Injectable, signal} from '@angular/core';
import {filter, takeUntil, tap} from "rxjs/operators";
import {ActivatedRoute, Router} from "@angular/router";
import {HttpClient} from "@angular/common/http";
import {AuthMe} from "../../AuthMe";
import {firstValueFrom} from "rxjs";

@Injectable({
  providedIn: 'root'
})
export class AuthService {

    #route = inject(ActivatedRoute);
    #router = inject(Router);
    #http = inject(HttpClient);
    
    account = signal<AuthMe | undefined>(undefined);
    isLoggedIn = computed(() => !!(this.account()?.clientPrincipal));
    #account = firstValueFrom(this.#http.get<AuthMe>('/.auth/me').pipe(tap(v => this.account.set(v))));
    
    getAccount(): Promise<AuthMe> {
        return this.#account;
    }
  
    login() {
        this.#router.navigate(['/login']);
    }
  
    logout() {
        window.location.assign('/.auth/logout');
    }
}

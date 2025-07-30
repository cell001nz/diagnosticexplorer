import {computed, inject, Injectable, signal} from '@angular/core';
import {ActivatedRoute, Router} from "@angular/router";
import {HttpClient} from "@angular/common/http";
import {firstValueFrom} from "rxjs";
import {AuthMe} from "@model/AuthMe";

@Injectable({
  providedIn: 'root'
})
export class AuthService {

    #route = inject(ActivatedRoute);
    #router = inject(Router);
    #http = inject(HttpClient);
    
    account = signal<AuthMe | undefined>(undefined);
    isLoggedIn = computed(() => !!(this.account()?.clientPrincipal));
    #account = firstValueFrom(this.#http.get<AuthMe>('/.auth/me'));       
    
    
    constructor() {
    }    
    
    getAccount(): Promise<AuthMe> {
        return this.#account;
    }
  
    login() {
        this.#router.navigate(['/login']);
    }
  
    logout() {
        window.location.assign('/.auth/logout');
    }
    async initialiseAsync() {
        this.#account.then(async v => {
            this.account.set(v);
            console.log(v);
            if (v.clientPrincipal) {
                let str = await firstValueFrom(this.#http.get('/api/Account/RegisterLogin', {responseType: 'text'}));
                console.log('LoggedIn', str);
            }
        });
    }
}

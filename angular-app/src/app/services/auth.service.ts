import {computed, inject, Injectable, signal} from '@angular/core';
import {ActivatedRoute, Router} from "@angular/router";
import {HttpClient} from "@angular/common/http";
import {firstValueFrom} from "rxjs";
import {AuthMe} from "@domain/AuthMe";
import {Account} from "@domain/Account";

@Injectable({
  providedIn: 'root'
})
export class AuthService {

    #route = inject(ActivatedRoute);
    #router = inject(Router);
    #http = inject(HttpClient);
    
    authMe = signal<AuthMe | undefined>(undefined);
    isLoggedIn = computed(() => !!(this.authMe()?.clientPrincipal));
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
    
    getStatus() : Promise<string> {
        return firstValueFrom(this.#http.get('/api/Account/Status', { responseType: 'text' }));
    }
    
    getMyAccount() : Promise<Account> {
        return firstValueFrom(this.#http.get<Account>('/api/Account/MyAccount'));
    }
    
    async initialiseAsync() {
        this.#account.then(async v => {
            this.authMe.set(v);
            console.log(v);
            if (v.clientPrincipal) {
                let str = await firstValueFrom(this.#http.get('/api/Account/RegisterLogin', {responseType: 'text'}));
                console.log('LoggedIn', str);
            }
        });
    }
}

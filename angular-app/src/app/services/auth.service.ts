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
    account = signal<Account | undefined>(undefined);
    isLoggedIn = computed(() => !!(this.authMe()?.clientPrincipal));
    isProfileComplete = computed(() => this.account()?.isProfileComplete ?? false);
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
        this.#router.navigate(['/logout']);
    }
    
    getStatus() : Promise<string> {
        return firstValueFrom(this.#http.get('/api/Account/Status', { responseType: 'text' }));
    }
    
    getMyAccount() : Promise<Account> {
        return firstValueFrom(this.#http.get<Account>('/api/Account/MyAccount'));
    }

    updateProfile(name: string, email: string): Promise<Account> {
        return firstValueFrom(this.#http.post<Account>('/api/Account/UpdateProfile', { name, email }));
    }
    
    async initialiseAsync() {
        const v = await this.#account;
        this.authMe.set(v);
        console.log(v);
        if (v.clientPrincipal) {
            let acct = await firstValueFrom(this.#http.get<Account>('/api/Account/RegisterLogin'));
            this.account.set(acct);
        }
    }
}

import {inject, Injectable, Injector} from "@angular/core";
import {ActivatedRouteSnapshot, CanActivate, GuardResult, MaybeAsync, Router, RouterStateSnapshot} from "@angular/router";
import {AuthService} from "./auth.service";

@Injectable({providedIn: 'root'})
export class LoginGuard implements CanActivate {
    #injector = inject(Injector);
    #auth = inject(AuthService);
    #router = inject(Router);
    
    canActivate(route: ActivatedRouteSnapshot,
                state: RouterStateSnapshot): MaybeAsync<GuardResult> {

        console.log('canActivate', state.url);
        try {
            const encodedRedirect = encodeURIComponent(state.url);
            if (this.#auth.isLoggedIn()) {
                // User is logged in — check if profile is complete
                const account = this.#auth.account();
                if (account && !account.isProfileComplete && !state.url.includes('complete-profile')) {
                    return this.#router.parseUrl('/app/complete-profile');
                }
                return true;
            }

            return this.#auth.getAccount()
                .then(acct => acct.clientPrincipal ? true : this.#router.parseUrl(`/login?post_login_redirect_uri=${encodedRedirect}`));

        } catch (err) {
            console.log(err);
            const encodedRedirect = encodeURIComponent(state.url);
            return this.#router.parseUrl(`/login?post_login_redirect_uri=${encodedRedirect}`);
        }
    }
}
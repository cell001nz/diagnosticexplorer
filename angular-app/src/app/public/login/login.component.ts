import {Component, inject} from '@angular/core';
import {ActivatedRoute, Router} from "@angular/router";
import {LandingLayout} from "@public/landing/components/landinglayout.component";

@Component({
  selector: 'app-login',
    imports: [
        LandingLayout
    ],
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss'
})
export class LoginComponent {

    #router = inject(Router);
    #route = inject(ActivatedRoute);
    private redirectUri: string | null = null;


    constructor() {
        this.#route.queryParams.subscribe(params => {
            this.redirectUri = params['post_login_redirect_uri'];
        });
    }


    login(provider: 'aad' | 'google') {
        const encodedRedirect = encodeURIComponent(this.redirectUri ?? '/app');
        window.location.href = `/.auth/login/${provider}?post_login_redirect_uri=${encodedRedirect}`;
    }
}

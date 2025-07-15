import {inject, Injectable, signal} from '@angular/core';
import {MSAL_GUARD_CONFIG, MsalBroadcastService, MsalGuardConfiguration, MsalService} from "@azure/msal-angular";
import {filter, takeUntil, tap} from "rxjs/operators";
import {
    AccountInfo,
    AuthenticationResult,
    EventMessage,
    EventType,
    InteractionStatus,
    InteractionType,
    PopupRequest,
    RedirectRequest
} from "@azure/msal-browser";
import {takeUntilDestroyed} from "@angular/core/rxjs-interop";
import {ActivatedRoute, Router} from "@angular/router";

@Injectable({
  providedIn: 'root'
})
export class AppAuthService {

 private msalGuardConfig = inject<MsalGuardConfiguration>(MSAL_GUARD_CONFIG);
    private authService = inject(MsalService);
    private msalBroadcastService = inject(MsalBroadcastService);
    private msalService = inject(MsalService);
    #route = inject(ActivatedRoute);
    #router = inject(Router);
    
    
    isLoggedIn = signal(false);
    account = signal<AccountInfo | undefined>(undefined);

    constructor() {
        /**
       * You can subscribe to MSAL events as shown below. For more info,
       * visit: https://github.com/AzureAD/microsoft-authentication-library-for-js/blob/dev/lib/msal-angular/docs/v2-docs/events.md
       */
      this.msalBroadcastService.inProgress$
        .pipe(
          filter((status: InteractionStatus) => status === InteractionStatus.None),
          takeUntilDestroyed()
        ).subscribe(() => {
          this.setLoginDisplay('inProgress$');
          
          this.checkAndSetActiveAccount();
        });
  
      this.msalBroadcastService.msalSubject$
        .pipe(
          filter((msg: EventMessage) => msg.eventType === EventType.LOGOUT_SUCCESS),
          takeUntilDestroyed()
        ).subscribe((result: EventMessage) => {
          this.setLoginDisplay('msalSubject$');
          this.checkAndSetActiveAccount();
        });
  
  
      this.msalBroadcastService.msalSubject$
        .pipe(
          filter((msg: EventMessage) => msg.eventType === EventType.LOGIN_SUCCESS),
          takeUntilDestroyed()
        ).subscribe((result: EventMessage) => {
          const payload = result.payload as AuthenticationResult;
          console.log('Active Account: ', payload)
          this.authService.instance.setActiveAccount(payload.account);
        });
    }
    
    async initialiseAsync(): Promise<void> {
        await this.authService.instance.initialize();
        this.msalService.handleRedirectObservable().subscribe();
        this.setLoginDisplay('initialiseAsync');
        this.authService.instance.enableAccountStorageEvents()
        
        // if (this.isLoggedIn() && this.#route.snapshot.url.length === 0)
        //   await this.#router.navigate(['app'])

    }

  
    setLoginDisplay(reason: string) {
        let allAccounts: AccountInfo[] = this.authService.instance.getAllAccounts();
        let first = allAccounts[0];
        
        this.isLoggedIn.set(allAccounts.length > 0);
        this.account.set(first); 
    }
  
    checkAndSetActiveAccount() {
      /**
       * If no active account set but there are accounts signed in, sets first account to active account
       * To use active account set here, subscribe to inProgress$ first in your component
       * Note: Basic usage demonstrated. Your app may require more complicated account selection logic
       */
      let activeAccount = this.authService.instance.getActiveAccount();
      console.log('checkAndSetActiveAccount', activeAccount);
  
      if (!activeAccount && this.authService.instance.getAllAccounts().length > 0) {
        let accounts = this.authService.instance.getAllAccounts();
        // add your code for handling multiple accounts here
        this.authService.instance.setActiveAccount(accounts[0]);
      }
    }
  
    login() {
      if (this.msalGuardConfig.interactionType === InteractionType.Popup) {
        if (this.msalGuardConfig.authRequest) {
          this.authService.loginPopup({
            ...this.msalGuardConfig.authRequest,
          } as PopupRequest)
            .subscribe((response: AuthenticationResult) => {
              this.authService.instance.setActiveAccount(response.account);
            });
        } else {
          this.authService.loginPopup()
            .subscribe((response: AuthenticationResult) => {
              this.authService.instance.setActiveAccount(response.account);
            });
        }
      } else {
        if (this.msalGuardConfig.authRequest) {
          this.authService.loginRedirect({
            ...this.msalGuardConfig.authRequest,
          } as RedirectRequest);
        } else {
          this.authService.loginRedirect();
        }
      }
    }
  
    logout() {
  
      if (this.msalGuardConfig.interactionType === InteractionType.Popup) {
        this.authService.logoutPopup({
          account: this.authService.instance.getActiveAccount(),
        });
      } else {
        this.authService.logoutRedirect({
          account: this.authService.instance.getActiveAccount(),
        });
      }
  }}

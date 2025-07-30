import { Routes } from '@angular/router';
import {AdminAccountsComponent} from "./admin-accounts/admin-accounts.component";

export default [
    { path: '', pathMatch: 'full', redirectTo: 'accounts' },
    { path: 'accounts', component: AdminAccountsComponent },
] as Routes;

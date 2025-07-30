import { Routes } from '@angular/router';
import {AccountComponent} from "./account/account.component";
import {NotAuthorizedComponent} from "../pages/not-authorized/not-authorized.component";
import {RoleGuard} from "@services/role-guard";
import {AppLayout} from "./app-layout/app.layout";
import {LoginGuard} from "@services/login-guard";
import {SitesComponent} from "./sites/sites.component";
import {EditSiteComponent} from "./edit-site/edit-site.component";
import {DocumentationComponent} from "@public/documentation/documentation.component";

export default [
    {path: '', component: AppLayout, canActivate: [LoginGuard], children: [
            {path: '', pathMatch: 'full', redirectTo: 'dashboard'},
            {path: 'account', component: AccountComponent},
            {path: 'sites', component: SitesComponent},
            {path: 'sites/new', component: EditSiteComponent, data: {createNew: true}},
            {path: 'sites/:id', component: EditSiteComponent, data: {createNew: false}},
            {path: 'diagnostics', loadChildren: () => import('./diagnostics/diagnostics.routes')},
            /*  
                { path: 'sites', children: [
                        { path: '', component: SitesComponent },
                        { path: ':id', component: EditSiteComponent, data: { editMode: true } },
                        { path: 'new', component: EditSiteComponent, data: { editMode: false }  },
                    ] },
            */
            {path: 'documentation', component: DocumentationComponent},
            {path: 'not-authorized', component: NotAuthorizedComponent},
            {path: 'admin', canActivate: [RoleGuard], loadChildren: () => import('./admin/admin.routes')}
        ]}
    ] as Routes;
import { Routes } from '@angular/router';

import { PanelsDemo } from './panelsdemo';
import {Notfound} from "../notfound/notfound";
import {FormLayoutDemo} from "./formlayoutdemo";

export default [
    { path: 'formlayout', data: { breadcrumb: 'Form Layout' }, component: FormLayoutDemo },
    { path: 'panel', data: { breadcrumb: 'Panel' }, component: PanelsDemo },
    { path: '**', component: Notfound }
] as Routes;

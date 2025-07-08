import { Routes } from '@angular/router';
import { Documentation } from './documentation/documentation';
import {Notfound} from "./notfound/notfound";

export default [
    { path: 'documentation', component: Documentation },
    { path: '**', component: Notfound }
] as Routes;

import { HttpClient } from '@angular/common/http';
import {inject, Injectable} from '@angular/core';
import {Observable} from "rxjs";

@Injectable({
  providedIn: 'root'
})
export class DataService {

  #http = inject(HttpClient)

  constructor() { }
  
  
  getData(): Observable<string> {
    return this.#http.get('http://localhost:4280/api/DataTrigger', { responseType: 'text' });
  }
  
}

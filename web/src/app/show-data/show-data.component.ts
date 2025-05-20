import {Component, inject, resource} from '@angular/core';
import {DataService} from "../services/data.service";
import {firstValueFrom} from "rxjs";
import {JsonPipe} from "@angular/common";
import {ErrMsgPipe} from "../pipes/err-msg.pipe";

@Component({
  selector: 'app-show-data',
  imports: [
    JsonPipe,
    ErrMsgPipe
  ],
  templateUrl: './show-data.component.html',
  styleUrl: './show-data.component.scss'
})
export class ShowDataComponent {

  #dataSvc = inject(DataService)
  
  myData = resource({
    defaultValue: "234",
    loader: ({}) => firstValueFrom(this.#dataSvc.getData())  
  })

  protected readonly Object = Object;
  protected readonly String = String;
}

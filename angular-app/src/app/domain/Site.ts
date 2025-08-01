import {Secret} from "./Secret";

export interface Site {
    
    id: string,
    name: string,
    secrets: Secret[];
}


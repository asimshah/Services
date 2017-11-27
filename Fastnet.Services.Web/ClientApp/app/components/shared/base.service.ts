import { Inject, OnInit } from '@angular/core';
import { Http, Response } from '@angular/http';
import { Observable } from 'rxjs/Observable';


import 'rxjs/add/operator/toPromise';
import 'rxjs/add/operator/map';
import 'rxjs/add/operator/catch';

class DataResult {
    data: any;
    exceptionMessage: string;
    message: string;
    success: boolean;
}

export abstract class BaseService {
    private baseUrl: string | null;
    private initialised: boolean = false;
    constructor(protected http: Http) {
        //console.log(`BaseService(): ${location.protocol}://${location.host}`);
        this.baseUrl = null;
        //console.log(`isRunninginNode = ${this.RunningInNode()} , originUrl: ${this.originUrl}`);
        if (this.RunningInNode()) {
            this.baseUrl = "http://localhost:50070";
        }
    }
    protected query(url: string): Promise<DataResult> {
        try {
            if (this.baseUrl != null) {
                url = `${this.baseUrl}/${url}`;
            }
            return this.http.get(url)
                .map(r => {
                    let dr = r.json() as DataResult;
                    if (!dr.success) {
                        console.log(`ErrorResult: ${JSON.stringify(dr)}`);
                    }
                    return dr;
                })
                .catch(this.handleError)
                .toPromise();
        } catch (e) {
            console.log(`http.get(${url}) failed, ${JSON.stringify(e)}`);
            let dr = new DataResult();
            dr.success = false;
            dr.exceptionMessage = "local http.get failure";
            return new Promise<DataResult>(r => r(dr));
        }
    }
    protected post(url: string, data: any): Promise<DataResult> {
        try {
            return this.http.post(url, data)
                .map(r => {
                    let dr = r.json() as DataResult;
                    if (!dr.success) {
                        console.log(`ErrorResult: ${JSON.stringify(dr)}`);
                    }
                    return dr;
                })
                .catch(this.handleError)
                .toPromise();
        } catch (e) {
            console.log(`http.post(${url}) failed, ${JSON.stringify(e)}`);
            let dr = new DataResult();
            dr.success = false;
            dr.exceptionMessage = "local http.post failure";
            return new Promise<DataResult>(r => r(dr));
        }
    }
    private handleError(error: any): Promise<any> {
        console.error('An error occurred', error);
        return Promise.reject(error.message || error);
    }
    private RunningInNode(): boolean {
        if (typeof window === 'undefined') {
            return true;
        }
        return false;
    }
}
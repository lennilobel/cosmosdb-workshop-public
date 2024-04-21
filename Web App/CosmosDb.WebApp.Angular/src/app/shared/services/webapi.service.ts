import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';

@Injectable()
export class WebApiService {

	constructor(
		private http: HttpClient,
	) {
	}

	private localdev = 'localdev';
	private azuredev = 'azuredev';
	private env = this.localdev;

	public InvokeGet(url: string, callback: any) {
		url = this.BuildFullUrl(url);
		this.http
			.get(url)
			.subscribe(
				data => callback({ isError: false, data: data }),
				error => callback({ isError: true, error: error }),
			);
	}

	public InvokeGetText(url: string, callback: any) {
		url = this.BuildFullUrl(url);
		this.http
			.get(url, { responseType: 'text' })
			.subscribe(
				data => callback({ isError: false, data: data }),
				error => callback({ isError: true, error: error }),
			);
	}

	public InvokePost(url: string, data: any, callback: any) {
		url = this.BuildFullUrl(url);
		this.http
			.post(url, data)
			.subscribe(
				data => callback({ isError: false, data: data }),
				error => callback({ isError: true, error: error }),
			);
	}

	public InvokeDelete(url: string, callback: any) {
		url = this.BuildFullUrl(url);
		this.http
			.delete(url)
			.subscribe(
				data => callback({ isError: false, data: data }),
				error => callback({ isError: true, error: error }),
			);
	}

	private BuildFullUrl(url: string) {
		switch (this.env) {
			case this.localdev:
				return `https://localhost:7081${url}`;
			case this.azuredev:
				return `https://cosmos-demos-api.azurewebsites.net${url}`;
		}
		return '';
	}

}

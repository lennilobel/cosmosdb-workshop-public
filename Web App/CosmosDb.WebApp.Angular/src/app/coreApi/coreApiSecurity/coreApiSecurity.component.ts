import { Component, ViewChild } from '@angular/core';
import { FormatterService } from 'src/app/shared/services/formatter.service';
import { WebApiService } from 'src/app/shared/services/webapi.service';
import { MessageBoxComponent } from 'src/app/shared/directives/messageBox/messageBox.component';

@Component({
	templateUrl: './coreApiSecurity.component.html',
	styleUrls: [
		'./coreApiSecurity.component.css',
		'../../app.component.css',
	]
})
export class CoreApiSecurityComponent {

	@ViewChild('alertMessage') private alertMessage!: MessageBoxComponent;
	public busyIndex: number = 0;
	public document: any;
	public documents!: any[] | null;
	public documentColumns!: any[];
	public alicePerm: any;
	public tomPerm: any;

	constructor(
		private webapi: WebApiService,
		public formatter: FormatterService,
	) {
	}

	public ViewUsers() {
		this.StartBusy();
		this.webapi.InvokeGet('/api/core/users', (result: any) => {
			this.EndBusy();
			if (result.isError) {
				this.alertMessage.ShowWebApiError(result.error);
				return;
			}
			this.documentColumns = ['UserId', 'ResourceId', 'SelfLink'];
			this.documents = result.data;
			this.document = null;
		});
	}

	public CreateUsers() {
		this.StartBusy();
		this.webapi.InvokeGetText('/api/core/createUsers', (result: any) => {
			this.EndBusy();
			if (result.isError) {
				this.alertMessage.ShowWebApiError(result.error);
				return;
			}
			this.document = result.data;
		});
	}

	public CreatePermissions() {
		this.StartBusy();
		this.webapi.InvokeGet('/api/core/createPermissions', (result: any) => {
			this.EndBusy();
			if (result.isError) {
				this.alertMessage.ShowWebApiError(result.error);
				return;
			}
			this.document = result.data.output;
			this.alicePerm = result.data.alicePerm;
			this.tomPerm = result.data.tomPerm;
		});
	}

	public ViewPermissions(username: string) {
		this.StartBusy();
		this.webapi.InvokeGet(`/api/core/permissions/${username}`, (result: any) => {
			this.EndBusy();
			if (result.isError) {
				this.alertMessage.ShowWebApiError(result.error);
				return;
			}
			this.documentColumns = ['PermissionId', 'ResourceId', 'PermissionMode', 'Token'];
			this.documents = result.data;
		});
	}

	public TestPermissions(username: string) {
		this.StartBusy();
		this.webapi.InvokeGetText(`/api/core/testPermissions/${username}`, (result: any) => {
			this.EndBusy();
			if (result.isError) {
				this.alertMessage.ShowWebApiError(result.error);
				return;
			}
			this.document = result.data;
		});
	}

	public DeleteUsers() {
		this.StartBusy();
		this.webapi.InvokeGetText('/api/core/deleteUsers', (result: any) => {
			this.EndBusy();
			if (result.isError) {
				this.alertMessage.ShowWebApiError(result.error);
				return;
			}
			this.document = result.data;
		});
	}

	private StartBusy() {
		this.document = null;
		this.documents = null;
		this.busyIndex++;
	}

	private EndBusy() {
		this.busyIndex--;
	}

}

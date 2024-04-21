import { Component, ViewChild } from '@angular/core';
import { WebApiService } from 'src/app/shared/services/webapi.service';
import { FormatterService } from 'src/app/shared/services/formatter.service';
import { MessageBoxComponent } from 'src/app/shared/directives/messageBox/messageBox.component';

@Component({
	templateUrl: './coreApiServer.component.html',
	styleUrls: [
		'./coreApiServer.component.css',
		'../../app.component.css',
	]
})
export class CoreApiServerComponent {

	@ViewChild('alertMessage') private alertMessage!: MessageBoxComponent;
	public busyIndex: number = 0;
	public document: any;
	public documents!: any[] | null;
	public documentColumns!: any[];

	constructor(
		public formatter: FormatterService,
		private webapi: WebApiService,
	) {
	}

	public CreateStoredProcedures() {
		this.StartBusy();
		this.webapi.InvokeGetText('/api/core/createSprocs', (result: any) => {
			this.EndBusy();
			if (result.isError) {
				this.alertMessage.ShowWebApiError(result.error);
				return;
			}
			this.document = result.data;
		});
	}

	public ViewStoredProcedures() {
		this.StartBusy();
		this.webapi.InvokeGet('/api/core/sprocs', (result: any) => {
			this.EndBusy();
			if (result.isError) {
				this.alertMessage.ShowWebApiError(result.error);
				return;
			}
			this.documentColumns = ['SprocId', 'SelfLink'];
			this.documents = result.data;
		});
	}

	public ExecuteStoredProcedure(sprocId: string) {
		this.StartBusy();
		this.webapi.InvokeGetText(`/api/core/executeSproc/${sprocId}`, (result: any) => {
			this.EndBusy();
			if (result.isError) {
				this.alertMessage.ShowWebApiError(result.error);
				return;
			}
			this.document = result.data;
		});
	}

	public DeleteStoredProcedures() {
		this.StartBusy();
		this.webapi.InvokeGetText('/api/core/deleteSprocs', (result: any) => {
			this.EndBusy();
			if (result.isError) {
				this.alertMessage.ShowWebApiError(result.error);
				return;
			}
			this.document = result.data;
		});
	}

	public CreateTriggers() {
		this.StartBusy();
		this.webapi.InvokeGetText('/api/core/createTriggers', (result: any) => {
			this.EndBusy();
			if (result.isError) {
				this.alertMessage.ShowWebApiError(result.error);
				return;
			}
			this.document = result.data;
		});
	}

	public ViewTriggers() {
		this.StartBusy();
		this.webapi.InvokeGet('/api/core/triggers', (result: any) => {
			this.EndBusy();
			if (result.isError) {
				this.alertMessage.ShowWebApiError(result.error);
				return;
			}
			this.documentColumns = ['TriggerId', 'SelfLink', 'Type', 'Operation'];
			this.documents = result.data;
		});
	}

	public ExecuteTrigger(triggerId: string) {
		this.StartBusy();
		this.webapi.InvokeGetText(`/api/core/executeTrigger/${triggerId}`, (result: any) => {
			this.EndBusy();
			if (result.isError) {
				this.alertMessage.ShowWebApiError(result.error);
				return;
			}
			this.document = result.data;
		});
	}

	public DeleteTriggers() {
		this.StartBusy();
		this.webapi.InvokeGetText('/api/core/deleteTriggers', (result: any) => {
			this.EndBusy();
			if (result.isError) {
				this.alertMessage.ShowWebApiError(result.error);
				return;
			}
			this.document = result.data;
		});
	}

	public CreateUserDefinedFunctions() {
		this.StartBusy();
		this.webapi.InvokeGetText('/api/core/createUdfs', (result: any) => {
			this.EndBusy();
			if (result.isError) {
				this.alertMessage.ShowWebApiError(result.error);
				return;
			}
			this.document = result.data;
		});
	}

	public ViewUserDefinedFunctions() {
		this.StartBusy();
		this.webapi.InvokeGet('/api/core/udfs', (result: any) => {
			this.EndBusy();
			if (result.isError) {
				this.alertMessage.ShowWebApiError(result.error);
				return;
			}
			this.documentColumns = ['UdfId', 'ResourceId', 'SelfLink'];
			this.documents = result.data;
		});
	}

	public ExecuteUserDefinedFunction(udfId: string) {
		this.StartBusy();
		this.webapi.InvokeGetText(`/api/core/executeUdf/${udfId}`, (result: any) => {
			this.EndBusy();
			if (result.isError) {
				this.alertMessage.ShowWebApiError(result.error);
				return;
			}
			this.document = result.data;
		});
	}

	public DeleteUserDefinedFunctions() {
		this.StartBusy();
		this.webapi.InvokeGetText('/api/core/deleteUdfs', (result: any) => {
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

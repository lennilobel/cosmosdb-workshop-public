import { Component, ViewChild } from '@angular/core';
import { WebApiService } from 'src/app/shared/services/webapi.service';
import { MessageBoxComponent } from 'src/app/shared/directives/messageBox/messageBox.component';

@Component({
	templateUrl: './coreApiSampleData.component.html',
	styleUrls: ['../../app.component.css']
})
export class CoreApiSampleDataComponent {

	@ViewChild('alertMessage') private alertMessage!: MessageBoxComponent;
	public busyIndex: number = 0;

	constructor(
		private webapi: WebApiService,
	) {
	}

	public CreateFamiliesCollection(demoType: string) {
		let url = `/api/core/familiesContainer/create/${demoType}`;
		this.CreateCollection(url);
	}

	public CreateMyStoreCollection(from: string) {
		let url = `/api/core/storesContainer/create/${from}`;
		this.CreateCollection(url);
	}

	private CreateCollection(url: string) {
		this.StartBusy();
		this.webapi.InvokeGetText(url, (result: any) => {
			this.EndBusy();
			if (result.isError) {
				this.alertMessage.ShowWebApiError(result.error);
				return;
			}
			this.alertMessage.Show("Done", result.data);
		});
	}

	private StartBusy() {
		this.busyIndex++;
	}

	private EndBusy() {
		this.busyIndex--;
	}

}

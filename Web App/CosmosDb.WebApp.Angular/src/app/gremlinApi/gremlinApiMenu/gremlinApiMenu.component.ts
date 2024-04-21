import { Component, ViewChild } from '@angular/core';
import { FormatterService } from 'src/app/shared/services/formatter.service';
import { WebApiService } from 'src/app/shared/services/webapi.service';
import { MessageBoxComponent } from 'src/app/shared/directives/messageBox/messageBox.component';

@Component({
	templateUrl: './gremlinApiMenu.component.html',
	styleUrls: ['../../app.component.css']
})
export class GremlinApiMenuComponent {

	@ViewChild('alertMessage') private alertMessage!: MessageBoxComponent;
	public busyIndex: number = 0;
	public document: any;

	constructor(
		private webapi: WebApiService,
		public formatter: FormatterService,
	) {
	}

	public PopulateAirportGraph() {
		this.StartBusy();
		this.webapi.InvokeGetText('/api/gremlin/airport/populate', (result: any) => {
			this.EndBusy();
			if (result.isError) {
				this.alertMessage.ShowWebApiError(result.error);
				return;
			}
			this.document = result.data;
		});
	}

	public QueryAirportGraph() {
		this.StartBusy();
		this.webapi.InvokeGetText('/api/gremlin/airport/query', (result: any) => {
			this.EndBusy();
			if (result.isError) {
				this.alertMessage.ShowWebApiError(result.error);
				return;
			}
			this.document = result.data;
		});
	}

	public PopulateComicBookGraph() {
		this.StartBusy();
		this.webapi.InvokeGetText('/api/gremlin/comicbook/populate', (result: any) => {
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
		this.busyIndex++;
	}

	private EndBusy() {
		this.busyIndex--;
	}

}

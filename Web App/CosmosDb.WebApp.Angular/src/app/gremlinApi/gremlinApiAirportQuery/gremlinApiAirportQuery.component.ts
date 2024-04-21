import { Component, ViewChild, OnInit } from '@angular/core';
import { FormatterService } from 'src/app/shared/services/formatter.service';
import { WebApiService } from 'src/app/shared/services/webapi.service';
import { MessageBoxComponent } from 'src/app/shared/directives/messageBox/messageBox.component';
import { FormGroup, FormBuilder } from '@angular/forms';

@Component({
	templateUrl: './gremlinApiAirportQuery.component.html',
	styleUrls: [
		'./gremlinApiAirportQuery.component.css',
		'../../app.component.css'
	]
})
export class GremlinApiAirportQueryComponent implements OnInit {

	@ViewChild('alertMessage') private alertMessage!: MessageBoxComponent;
	public busyIndex: number = 0;
	public gates!: any[];
	public choices!: any[];
	public criteriaForm!: FormGroup;

	constructor(
		private formBuilder: FormBuilder,
		private webapi: WebApiService,
		public formatter: FormatterService,
	) {
	}

	public ngOnInit() {
		this.criteriaForm = this.formBuilder.group({
			ArrivalGate: [],
			DepartureGate: [],
			FirstSwitchTerminalsThenEat: [],
			MinYelpRating: [],
			//ArrivalGate: ['Gate T1-2'],
			//DepartureGate: ['Gate T2-3'],
			//FirstSwitchTerminalsThenEat: [false],
			//MinYelpRating: [4],
		});
		this.GetGates();
	}

	private GetGates() {
		this.StartBusy();
		this.webapi.InvokeGet('/api/gremlin/airport/gates', (result: any) => {
			this.EndBusy();
			if (result.isError) {
				this.alertMessage.ShowWebApiError(result.error);
				return;
			}
			this.gates = result.data;
		});
	}

	public FindFood() {
		this.busyIndex++;
		this.webapi.InvokePost('/api/gremlin/airport/query', this.criteriaForm.value, (result: any) => {
			this.busyIndex--;
			if (result.isError) {
				this.alertMessage.ShowWebApiError(result.error);
				return;
			}
			this.choices = this.TransformResults(result.data);
			console.log(this.choices);
		});
	}

	private TransformResults(results: any) {
		const choices = [];
		for (let i = 0; i < results.length; i++) {
			const result = results[i];
			const resultSteps = result.objects;
			let totalDistanceInMinutes = 0;
			const choice = {
				isExpanded: false,
				restaurant: null,
				imageB64: null,
				rating: null,
				averagePrice: null,
				totalDistanceInMinutes: null as any,
				steps: [] as any[],
			};
			for (let j = 0; j < resultSteps.length; j++) {
				let resultStep = resultSteps[j];
				if (resultStep.type == 'vertex') {
					let step = {
						action: (j == 0 ? "start" : (j == resultSteps.length - 1 ? "end" : "go")),
						vertexType: resultStep.label,
						vertexName: resultStep.id,
						edgeType: null,
						stepDistanceInMinutes: null,
						totalDistanceInMinutes: null,
					}
					if (resultStep.label == 'restaurant') {
						choice.restaurant = resultStep.id;
						choice.imageB64 = resultStep.properties.imageB64[0].value;
						choice.rating = resultStep.properties.rating[0].value;
						choice.averagePrice = resultStep.properties.averagePrice[0].value;
					}
					choice.steps.push(step);
				}
				else {
					const distanceInMinutes = resultStep.properties.distanceInMinutes;
					let step = choice.steps[choice.steps.length - 1];
					step.totalDistanceInMinutes = totalDistanceInMinutes;
					step.stepDistanceInMinutes = distanceInMinutes;
					totalDistanceInMinutes += distanceInMinutes;
					step.edgeType = resultStep.label;
				}
			}
			choice.totalDistanceInMinutes = totalDistanceInMinutes;
			choices.push(choice);
		}
		return choices;
	}

	private StartBusy() {
		this.busyIndex++;
	}

	private EndBusy() {
		this.busyIndex--;
	}

}

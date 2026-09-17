import { Component, EventEmitter, Input, Output } from '@angular/core';
import { FormsModule } from '@angular/forms';

import { SmsTestPhone } from '../template.models';

export type TestSmsPanelSection = 'action' | 'status' | 'recipient';

@Component({
  selector: 'app-test-sms-panel',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './test-sms-panel.html',
  styleUrl: './test-sms-panel.scss',
})
export class TestSmsPanel {
  @Input({ required: true }) section!: TestSmsPanelSection;
  @Input({ required: true }) testPhones!: SmsTestPhone[];
  @Input({ required: true }) phonesLoading!: boolean;
  @Input({ required: true }) phonesError!: string;
  @Input({ required: true }) selectedPhone!: string;
  @Input({ required: true }) testStatus!: string;
  @Input() statusTone: 'success' | 'error' = 'success';
  @Input() sending = false;

  @Output() readonly testPhoneSelected = new EventEmitter<string>();
  @Output() readonly testSmsRequested = new EventEmitter<void>();
}

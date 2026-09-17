import { Component, Input } from '@angular/core';

import { SmsSegmentStats } from '../sms-segments';

@Component({
  selector: 'app-phone-preview',
  standalone: true,
  imports: [],
  templateUrl: './phone-preview.html',
  styleUrl: './phone-preview.scss',
  host: {
    class: 'template-phone-shell',
    'aria-label': 'תצוגת הודעת SMS',
  },
})
export class PhonePreview {
  @Input({ required: true }) senderName!: string;
  @Input({ required: true }) direction!: 'rtl' | 'ltr';
  @Input({ required: true }) renderedMessage!: string;
  @Input({ required: true }) emptyMessage!: string;
  @Input({ required: true }) messageStats!: SmsSegmentStats;
}

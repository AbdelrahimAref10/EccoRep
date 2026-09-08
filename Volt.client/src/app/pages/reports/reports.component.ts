import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { TranslatePipe } from '../../shared/pipes/translate.pipe';

interface ReportItem {
  route: string;
  titleKey: string;
  descriptionKey: string;
}

@Component({
  selector: 'app-reports',
  standalone: true,
  imports: [CommonModule, RouterModule, TranslatePipe],
  templateUrl: './reports.component.html',
  styleUrls: ['./reports.component.css', './report-page-shared.css']
})
export class ReportsComponent {
  reportItems: ReportItem[] = [
    {
      route: 'orders-details',
      titleKey: 'reports.ordersDetails',
      descriptionKey: 'reports.ordersDetailsDesc'
    },
    {
      route: 'cancelled-orders',
      titleKey: 'reports.cancelledOrders',
      descriptionKey: 'reports.cancelledOrdersDesc'
    },
    {
      route: 'cancellation-debts',
      titleKey: 'reports.cancellationDebts',
      descriptionKey: 'reports.cancellationDebtsDesc'
    },
    {
      route: 'payments',
      titleKey: 'reports.payments',
      descriptionKey: 'reports.paymentsDesc'
    },
    {
      route: 'paypal-refunds',
      titleKey: 'reports.paypalRefunds',
      descriptionKey: 'reports.paypalRefundsDesc'
    }
  ];
}

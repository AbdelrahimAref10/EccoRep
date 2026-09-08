import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, RouterOutlet } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { TranslatePipe } from '../../shared/pipes/translate.pipe';

@Component({
  selector: 'app-merchant-layout',
  standalone: true,
  imports: [CommonModule, RouterModule, RouterOutlet, TranslatePipe],
  templateUrl: './merchant-layout.component.html',
  styleUrl: './merchant-layout.component.css'
})
export class MerchantLayoutComponent {
  constructor(private authService: AuthService) {}

  logout(): void {
    this.authService.logout();
  }
}

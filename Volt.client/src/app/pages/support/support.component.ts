import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import {
  AdminSupportClient,
  SupportDto,
  CreateSupportCommand,
  UpdateSupportCommand
} from '../../core/services/clientAPI';
import { LocaleService } from '../../core/services/locale.service';
import { TranslatePipe } from '../../shared/pipes/translate.pipe';

@Component({
  selector: 'app-support',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, TranslatePipe],
  templateUrl: './support.component.html',
  styleUrls: ['./support.component.css', '../../shared/styles/entity-form.css']
})
export class SupportComponent implements OnInit {
  private readonly localeService = inject(LocaleService);
  private readonly supportClient = inject(AdminSupportClient);
  private readonly fb = inject(FormBuilder);

  supportForm: FormGroup = this.fb.group({
    companyName: ['', [Validators.required, Validators.minLength(2)]],
    address: [''],
    phoneNumber: [''],
    whatsAppNumber: [''],
    email: ['', [Validators.email]],
    websiteUrl: [''],
    facebookUrl: [''],
    instagramUrl: [''],
    twitterUrl: [''],
    linkedInUrl: [''],
    tikTokUrl: [''],
    youTubeUrl: [''],
    workingHours: [''],
    latitude: [null as number | null],
    longitude: [null as number | null],
    additionalInfo: ['']
  });

  supportId: number | null = null;
  isEditMode = false;
  isLoading = true;
  isSaving = false;
  errorMessage = '';
  successMessage = '';

  ngOnInit(): void {
    this.loadSupport();
  }

  loadSupport(): void {
    this.isLoading = true;
    this.errorMessage = '';
    this.successMessage = '';

    this.supportClient.get().subscribe({
      next: (support: SupportDto) => {
        this.isEditMode = true;
        this.supportId = support.supportId;
        this.patchForm(support);
        this.isLoading = false;
      },
      error: () => {
        this.isEditMode = false;
        this.supportId = null;
        this.isLoading = false;
      }
    });
  }

  onSubmit(): void {
    if (this.supportForm.invalid || this.isSaving) {
      this.supportForm.markAllAsTouched();
      return;
    }

    this.isSaving = true;
    this.errorMessage = '';
    this.successMessage = '';

    const value = this.supportForm.getRawValue();
    const emptyToNull = (v: string | null | undefined) =>
      v == null || String(v).trim() === '' ? null : String(v).trim();

    if (this.isEditMode && this.supportId) {
      const command = new UpdateSupportCommand();
      command.supportId = this.supportId;
      command.companyName = value.companyName.trim();
      command.address = emptyToNull(value.address);
      command.phoneNumber = emptyToNull(value.phoneNumber);
      command.whatsAppNumber = emptyToNull(value.whatsAppNumber);
      command.email = emptyToNull(value.email);
      command.websiteUrl = emptyToNull(value.websiteUrl);
      command.facebookUrl = emptyToNull(value.facebookUrl);
      command.instagramUrl = emptyToNull(value.instagramUrl);
      command.twitterUrl = emptyToNull(value.twitterUrl);
      command.linkedInUrl = emptyToNull(value.linkedInUrl);
      command.tikTokUrl = emptyToNull(value.tikTokUrl);
      command.youTubeUrl = emptyToNull(value.youTubeUrl);
      command.workingHours = emptyToNull(value.workingHours);
      command.latitude = value.latitude === '' || value.latitude == null ? null : Number(value.latitude);
      command.longitude = value.longitude === '' || value.longitude == null ? null : Number(value.longitude);
      command.additionalInfo = emptyToNull(value.additionalInfo);

      this.supportClient.update(this.supportId, command).subscribe({
        next: (support) => {
          this.patchForm(support);
          this.successMessage = this.localeService.translate('support.saved');
          this.isSaving = false;
        },
        error: (error: any) => {
          this.errorMessage = this.extractErrorMessage(error)
            || this.localeService.translate('support.failedToSave');
          this.isSaving = false;
        }
      });
      return;
    }

    const command = new CreateSupportCommand();
    command.companyName = value.companyName.trim();
    command.address = emptyToNull(value.address);
    command.phoneNumber = emptyToNull(value.phoneNumber);
    command.whatsAppNumber = emptyToNull(value.whatsAppNumber);
    command.email = emptyToNull(value.email);
    command.websiteUrl = emptyToNull(value.websiteUrl);
    command.facebookUrl = emptyToNull(value.facebookUrl);
    command.instagramUrl = emptyToNull(value.instagramUrl);
    command.twitterUrl = emptyToNull(value.twitterUrl);
    command.linkedInUrl = emptyToNull(value.linkedInUrl);
    command.tikTokUrl = emptyToNull(value.tikTokUrl);
    command.youTubeUrl = emptyToNull(value.youTubeUrl);
    command.workingHours = emptyToNull(value.workingHours);
    command.latitude = value.latitude === '' || value.latitude == null ? null : Number(value.latitude);
    command.longitude = value.longitude === '' || value.longitude == null ? null : Number(value.longitude);
    command.additionalInfo = emptyToNull(value.additionalInfo);

    this.supportClient.create(command).subscribe({
      next: (support) => {
        this.isEditMode = true;
        this.supportId = support.supportId;
        this.patchForm(support);
        this.successMessage = this.localeService.translate('support.saved');
        this.isSaving = false;
      },
      error: (error: any) => {
        this.errorMessage = this.extractErrorMessage(error)
          || this.localeService.translate('support.failedToSave');
        this.isSaving = false;
      }
    });
  }

  private patchForm(support: SupportDto): void {
    this.supportForm.patchValue({
      companyName: support.companyName ?? '',
      address: support.address ?? '',
      phoneNumber: support.phoneNumber ?? '',
      whatsAppNumber: support.whatsAppNumber ?? '',
      email: support.email ?? '',
      websiteUrl: support.websiteUrl ?? '',
      facebookUrl: support.facebookUrl ?? '',
      instagramUrl: support.instagramUrl ?? '',
      twitterUrl: support.twitterUrl ?? '',
      linkedInUrl: support.linkedInUrl ?? '',
      tikTokUrl: support.tikTokUrl ?? '',
      youTubeUrl: support.youTubeUrl ?? '',
      workingHours: support.workingHours ?? '',
      latitude: support.latitude,
      longitude: support.longitude,
      additionalInfo: support.additionalInfo ?? ''
    });
  }

  private extractErrorMessage(error: any): string {
    const result = error?.result;
    if (result?.errorMessage) {
      return result.errorMessage;
    }
    if (typeof error?.message === 'string' && !error.message.includes('server side')) {
      return error.message;
    }
    return '';
  }
}

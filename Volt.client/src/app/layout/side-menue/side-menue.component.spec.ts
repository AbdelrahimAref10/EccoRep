import { ComponentFixture, TestBed } from '@angular/core/testing';

import { SideMenueComponent } from './side-menue.component';

describe('SideMenueComponent', () => {
  let component: SideMenueComponent;
  let fixture: ComponentFixture<SideMenueComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SideMenueComponent]
    })
    .compileComponents();
    
    fixture = TestBed.createComponent(SideMenueComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});

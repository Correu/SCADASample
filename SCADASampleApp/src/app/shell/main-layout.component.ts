import { CommonModule } from '@angular/common';
import { Component, inject, OnInit } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '../core/auth.service';
import { ProcessHubService } from '../core/process-hub.service';

@Component({
  selector: 'app-main-layout',
  standalone: true,
  imports: [CommonModule, RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './main-layout.component.html',
  styleUrl: './main-layout.component.css',
})
export class MainLayoutComponent implements OnInit {
  readonly auth = inject(AuthService);
  private readonly hub = inject(ProcessHubService);

  ngOnInit(): void {
    void this.hub.ensureConnected();
  }

  logout(): void {
    void this.hub.disconnect();
    this.auth.logout();
  }
}

import { isPlatformBrowser } from '@angular/common';
import { inject, Injectable, OnDestroy, PLATFORM_ID } from '@angular/core';
import {
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
} from '@microsoft/signalr';
import { Subject } from 'rxjs';
import { environment } from '../../environments/environment';
import { LocationUpdate, TagValueUpdate, TransferUpdate } from '../models/api.models';
import { AuthService } from './auth.service';

@Injectable({ providedIn: 'root' })
export class ProcessHubService implements OnDestroy {
  private readonly auth = inject(AuthService);
  private readonly platformId = inject(PLATFORM_ID);

  private hub?: HubConnection;

  readonly tagUpdates = new Subject<TagValueUpdate>();
  readonly locationUpdates = new Subject<LocationUpdate>();
  readonly transferUpdates = new Subject<TransferUpdate>();

  async ensureConnected(): Promise<void> {
    if (!isPlatformBrowser(this.platformId)) {
      return;
    }

    const token = this.auth.token();
    if (!token) {
      await this.disconnect();
      return;
    }

    if (this.hub?.state === HubConnectionState.Connected) {
      return;
    }

    await this.disconnect();

    this.hub = new HubConnectionBuilder()
      .withUrl(`${environment.apiUrl}/hubs/process`, {
        accessTokenFactory: () => this.auth.token() ?? '',
      })
      .withAutomaticReconnect()
      .build();

    this.hub.on('TagUpdate', (payload: TagValueUpdate) => {
      this.tagUpdates.next(payload);
    });

    this.hub.on('LocationUpdate', (payload: LocationUpdate) => {
      this.locationUpdates.next(payload);
    });

    this.hub.on('TransferUpdate', (payload: TransferUpdate) => {
      this.transferUpdates.next(payload);
    });

    try {
      await this.hub.start();
    } catch {
      // Retry on next navigation or manual ensureConnected
    }
  }

  async disconnect(): Promise<void> {
    if (this.hub) {
      try {
        await this.hub.stop();
      } catch {
        /* ignore */
      }
      this.hub = undefined;
    }
  }

  ngOnDestroy(): void {
    void this.disconnect();
  }
}

import { Component, OnInit } from '@angular/core';
import { Router, ActivatedRoute, Params } from '@angular/router';

import 'rxjs/add/operator/switchMap';

import { BsModalService } from 'ngx-bootstrap/modal';

import { ChatRoomListService } from './chat-room-list.service';
import { ChatRoomDto } from '../dtos/chat-room.dto';
import { ChatRoomCreateComponent } from '../chat-room-create/chat-room-create.component';

@Component({
  selector: 'app-chat-room-list',
  templateUrl: './chat-room-list.component.html',
  styleUrls: ['./chat-room-list.component.css']
})
export class ChatRoomListComponent implements OnInit {

  keyword: string;

  roomList: ChatRoomDto[];

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private chatRoomListService: ChatRoomListService,
    private modalService: BsModalService
  ) {}

  ngOnInit() {
    this.route.params
      .subscribe((params: Params) => {
        this.keyword = params['keyword'];
        const page = +params['page'] || 1;
        this.chatRoomListService.getList(this.keyword, page)
          .subscribe(data => this.roomList = data.chatRoomList);
      });
  }

  /**
   * 根据关键词进行检索
   */
  search() {
    const params = this.keyword ? {keyword: this.keyword, page: 1} : {page: 1};
    this.router.navigate(['../rooms', params]);
  }

  /**
   * 创建房间
   */
  create() {
    this.modalService.show(ChatRoomCreateComponent, {backdrop: 'static'});
  }
}

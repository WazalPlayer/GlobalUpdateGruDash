-- Кланы для Supabase. Выполнить один раз: Supabase → SQL Editor → вставить и Run.
-- Правила: клан создаётся бесплатно, тип вступления open (по умолчанию) или request (по заявке),
-- лимит участников 50, в одном клане может состоять только один игрок.

create table if not exists clans (
  id bigserial primary key,
  name text unique not null check (char_length(name) between 2 and 24),
  tag text unique not null check (char_length(tag) between 2 and 5),
  leader_id text not null,
  join_type text not null default 'open' check (join_type in ('open', 'request')),
  member_limit int not null default 50,
  stars bigint not null default 0,
  created_at timestamptz not null default now()
);

create table if not exists clan_members (
  clan_id bigint not null references clans(id) on delete cascade,
  player_id text primary key,            -- primary key: один игрок — один клан
  role text not null default 'member' check (role in ('leader', 'member')),
  joined_at timestamptz not null default now()
);

create table if not exists clan_requests (
  clan_id bigint not null references clans(id) on delete cascade,
  player_id text not null,
  created_at timestamptz not null default now(),
  primary key (clan_id, player_id)
);

create table if not exists clan_messages (
  id bigserial primary key,
  clan_id bigint not null references clans(id) on delete cascade,
  player_id text not null,
  player_name text not null default '',
  text text not null check (char_length(text) between 1 and 200),
  created_at timestamptz not null default now()
);

create index if not exists clan_messages_clan_idx on clan_messages(clan_id, created_at desc);
create index if not exists clans_stars_idx on clans(stars desc);

-- Создание клана (бесплатно)
create or replace function create_clan(p_name text, p_tag text, p_join_type text, p_player text)
returns bigint language plpgsql as $$
declare new_id bigint;
begin
  if exists (select 1 from clan_members where player_id = p_player) then
    raise exception 'already_in_clan';
  end if;
  if p_join_type not in ('open', 'request') then
    raise exception 'bad_join_type';
  end if;
  insert into clans(name, tag, leader_id, join_type) values (p_name, p_tag, p_player, p_join_type)
    returning id into new_id;
  insert into clan_members(clan_id, player_id, role) values (new_id, p_player, 'leader');
  return new_id;
end $$;

-- Вступление: open — сразу, request — заявка. Возвращает: joined / requested / full / already_in_clan / not_found
create or replace function join_clan(p_clan bigint, p_player text)
returns text language plpgsql as $$
declare c clans%rowtype; cnt int;
begin
  if exists (select 1 from clan_members where player_id = p_player) then return 'already_in_clan'; end if;
  select * into c from clans where id = p_clan;
  if not found then return 'not_found'; end if;
  select count(*) into cnt from clan_members where clan_id = p_clan;
  if cnt >= c.member_limit then return 'full'; end if;
  if c.join_type = 'open' then
    insert into clan_members(clan_id, player_id) values (p_clan, p_player);
    return 'joined';
  end if;
  insert into clan_requests(clan_id, player_id) values (p_clan, p_player) on conflict do nothing;
  return 'requested';
end $$;

-- Лидер принимает заявку
create or replace function approve_request(p_clan bigint, p_leader text, p_player text)
returns text language plpgsql as $$
declare c clans%rowtype; cnt int;
begin
  select * into c from clans where id = p_clan;
  if not found or c.leader_id <> p_leader then raise exception 'not_leader'; end if;
  select count(*) into cnt from clan_members where clan_id = p_clan;
  if cnt >= c.member_limit then return 'full'; end if;
  if exists (select 1 from clan_members where player_id = p_player) then
    delete from clan_requests where clan_id = p_clan and player_id = p_player;
    return 'already_in_clan';
  end if;
  insert into clan_members(clan_id, player_id) values (p_clan, p_player);
  delete from clan_requests where player_id = p_player;
  return 'approved';
end $$;

-- Выход (лидер выйти не может — сначала удалить клан или передать лидерство)
create or replace function leave_clan(p_player text)
returns void language plpgsql as $$
begin
  if exists (select 1 from clan_members where player_id = p_player and role = 'leader') then
    raise exception 'leader_cannot_leave';
  end if;
  delete from clan_members where player_id = p_player;
end $$;

-- Доступ. ВНИМАНИЕ: без входа в аккаунт Supabase любой с anon-ключом может читать и писать.
-- Это временно, пока нет нормальной авторизации игроков.
alter table clans enable row level security;
alter table clan_members enable row level security;
alter table clan_requests enable row level security;
alter table clan_messages enable row level security;

drop policy if exists "clans read" on clans;
create policy "clans read" on clans for select using (true);
drop policy if exists "clans write" on clans;
create policy "clans write" on clans for all using (true) with check (true);
drop policy if exists "clan_members read" on clan_members;
create policy "clan_members read" on clan_members for select using (true);
drop policy if exists "clan_members write" on clan_members;
create policy "clan_members write" on clan_members for all using (true) with check (true);
drop policy if exists "clan_requests all" on clan_requests;
create policy "clan_requests all" on clan_requests for all using (true) with check (true);
drop policy if exists "clan_messages read" on clan_messages;
create policy "clan_messages read" on clan_messages for select using (true);
drop policy if exists "clan_messages insert" on clan_messages;
create policy "clan_messages insert" on clan_messages for insert with check (true);

-- Роспуск клана лидером (удаляет клан, участников, заявки и сообщения)
create or replace function disband_clan(p_player text)
returns void language plpgsql as $$
begin
  if not exists (select 1 from clan_members where player_id = p_player and role = 'leader') then
    raise exception 'not_leader';
  end if;
  delete from clans where leader_id = p_player;
end $$;

-- Статистика клана: участники и сумма звёзд всех участников (для топа и карточки клана)
create or replace view clan_stats as
select c.id as clan_id, c.name, c.tag, c.join_type, c.leader_id, c.member_limit,
       count(m.player_id) as members,
       coalesce(sum(a.stars), 0) as total_stars
from clans c
left join clan_members m on m.clan_id = c.id
left join accounts a on a.player_id = m.player_id
group by c.id;

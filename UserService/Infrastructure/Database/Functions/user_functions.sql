create or replace function exists_user_by_login(input_login varchar(100))
returns boolean
language sql
stable
as $$
    select exists(
        select 1
        from users as u
        where u.login = input_login
    );
$$;

create or replace function create_user(
    input_login varchar(100),
    input_password_hash varchar(100),
    input_password_salt varchar(100),
    input_name varchar(100),
    input_surname varchar(100),
    input_age int)
returns table(
    id int,
    login varchar(100),
    password_hash varchar(100),
    password_salt varchar(100),
    name varchar(100),
    surname varchar(100),
    age int)
language sql
as $$
    insert into users(login, password_hash, password_salt, name, surname, age)
    values (input_login, input_password_hash, input_password_salt, input_name, input_surname, input_age)
    returning users.id, users.login, users.password_hash, users.password_salt,
              users.name, users.surname, users.age;
$$;

create or replace function get_user_by_id(input_id int)
returns table(
    id int,
    login varchar(100),
    password_hash varchar(100),
    password_salt varchar(100),
    name varchar(100),
    surname varchar(100),
    age int)
language sql
stable
as $$
    select u.id, u.login, u.password_hash, u.password_salt, u.name, u.surname, u.age
    from users as u
    where u.id = input_id;
$$;

create or replace function get_user_by_name(input_name varchar(100), input_surname varchar(100))
returns table(
    id int,
    login varchar(100),
    password_hash varchar(100),
    password_salt varchar(100),
    name varchar(100),
    surname varchar(100),
    age int)
language sql
stable
as $$
    select u.id, u.login, u.password_hash, u.password_salt, u.name, u.surname, u.age
    from users as u
    where u.name = input_name and u.surname = input_surname
    order by u.id;
$$;

create or replace function update_user(
    input_id int,
    input_password_hash varchar(100),
    input_password_salt varchar(100),
    input_name varchar(100),
    input_surname varchar(100),
    input_age int)
returns table(
    id int,
    login varchar(100),
    password_hash varchar(100),
    password_salt varchar(100),
    name varchar(100),
    surname varchar(100),
    age int)
language sql
as $$
    update users as u
    set password_hash = coalesce(input_password_hash, u.password_hash),
        password_salt = coalesce(input_password_salt, u.password_salt),
        name = coalesce(input_name, u.name),
        surname = coalesce(input_surname, u.surname),
        age = coalesce(input_age, u.age)
    where u.id = input_id
    returning u.id, u.login, u.password_hash, u.password_salt, u.name, u.surname, u.age;
$$;

create or replace function delete_user(input_id int)
returns boolean
language plpgsql
as $$
begin
    delete from users as u
    where u.id = input_id;

    return found;
end;
$$;

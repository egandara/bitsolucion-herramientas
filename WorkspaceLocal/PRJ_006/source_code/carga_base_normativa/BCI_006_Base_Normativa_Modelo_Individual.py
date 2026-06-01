# Databricks notebook source
# MAGIC %md
# MAGIC # BCI_006_Base_Normativa_Modelo_Individual
# MAGIC **************************************************************************

# COMMAND ----------

# MAGIC %md
# MAGIC ## Información del Notebook

# COMMAND ----------

# MAGIC %md
# MAGIC ### Encabezado
# MAGIC ***************************************************************************************************************
# MAGIC * Nombre: BCI_006_Base_Normativa_Modelo_Individual.ipynb
# MAGIC * Ruta: 
# MAGIC * Autor: BitSolucion Spa
# MAGIC * Ing. SW BCI: <alguien.de.ti@bci.cl> - Marcelo Lizama <marcelo.lizama@bci.cl> - Ivan Jara <ivan.jara@bci.cl>
# MAGIC * Fecha: 22/12/2025
# MAGIC * Descripción: Notebook encargado de generar el modelo individual Interno y Estandar, además se Utilizan las colocaciones comerciales para identificar Operaciones Repetidas
# MAGIC * Documentación: 
# MAGIC ***************************************************************************************************************

# COMMAND ----------

# MAGIC %md
# MAGIC ### Mantenciones
# MAGIC **************************************************************************
# MAGIC #### Mantención Nro: 
# MAGIC * Autor: <Nombre Autor> (<Empresa del Autor (Bci/Otra)>) - Ing. SW BCI: <Nombre Ing. SW BCI>
# MAGIC * Fecha: <dd/mm/yyyy> 
# MAGIC * Descripción: <Descripción de la mantención>      
# MAGIC ***************************************************************************

# COMMAND ----------

# MAGIC %md
# MAGIC ### Tablas Entrada y Salida
# MAGIC **************************************************************************
# MAGIC #### Tablas Entrada: 
# MAGIC * slv_RiesgoCred_RiesgoCredEmp_db.prov_emp_per
# MAGIC * slv_RiesgoCred_RiesgoCredEmp_db.mitcalcprov
# MAGIC * tabmae_tablapi_sbif
# MAGIC * tmp_colcomer
# MAGIC ***************************************************************************
# MAGIC #### Tablas Salida: 
# MAGIC * tmp_modelo_indiv
# MAGIC * tmp_individual_repetidos

# COMMAND ----------

# MAGIC %md
# MAGIC ## Captura de Variable

# COMMAND ----------

# MAGIC %md
# MAGIC ### Crear Widgets para Capturar de Variables

# COMMAND ----------

dbutils.widgets.removeAll()

dbutils.widgets.text("fechaProcesoW","","01 Fecha Proceso:")
dbutils.widgets.text("db_platinumW","","02 platinum DB:")
dbutils.widgets.text("db_plat_tempW","","03 platinum temp db:")
dbutils.widgets.text("db_RiesgoCredEmpW","","04 RiesgoCredEmp DB:")
dbutils.widgets.text("db_slv_ParametricasW","","05 Parametricas DB:")
dbutils.widgets.text("db_location_plat_tempW","","06 platinum temp db:")

# COMMAND ----------

# MAGIC %md
# MAGIC ### Asignar Objeto a Lectura de Widgets y Variables

# COMMAND ----------

fechaProcesoX = dbutils.widgets.get("fechaProcesoW")
spark.conf.set("bci.fechaProcesoX", fechaProcesoX)

db_platinumX = dbutils.widgets.get("db_platinumW")
spark.conf.set("bci.db_platinumX", db_platinumX)

db_plat_tempX = dbutils.widgets.get("db_plat_tempW")
spark.conf.set("bci.db_plat_tempX", db_plat_tempX)

db_RiesgoCredEmpX = dbutils.widgets.get("db_RiesgoCredEmpW")
spark.conf.set("bci.db_RiesgoCredEmpX", db_RiesgoCredEmpX)

db_slv_ParametricasX = dbutils.widgets.get("db_slv_ParametricasW")
spark.conf.set("bci.db_slv_ParametricasX", db_slv_ParametricasX)

db_location_plat_tempX = dbutils.widgets.get("db_location_plat_tempW")
spark.conf.set("bci.db_location_plat_tempX", db_location_plat_tempX)

print("Parámetros")
print("fechaProcesoX: " + fechaProcesoX)
print("db_platinumX: " + db_platinumX)
print("db_plat_tempX: " + db_plat_tempX)
print("db_RiesgoCredEmpX: " + db_RiesgoCredEmpX)
print("db_slv_ParametricasX: " + db_slv_ParametricasX)

# COMMAND ----------

# MAGIC %md
# MAGIC ## Carga de funciones

# COMMAND ----------

# MAGIC %run "../../Funciones"

# COMMAND ----------

# MAGIC %md
# MAGIC ### Funcion Conexion Teradata

# COMMAND ----------

# MAGIC %md
# MAGIC ## Validación de ingreso de parámetros

# COMMAND ----------

# MAGIC %md
# MAGIC #### Validacion Parametro Vacio

# COMMAND ----------

valida_param_vacio(fechaProcesoX,'fechaProcesoX')
valida_param_vacio(db_platinumX,'db_platinumX')
valida_param_vacio(db_plat_tempX,'db_plat_tempX')
valida_param_vacio(db_RiesgoCredEmpX,'db_RiesgoCredEmpX')
valida_param_vacio(db_slv_ParametricasX,'db_slv_ParametricasX')
valida_param_vacio(db_location_plat_tempX,'db_location_plat_tempX')


# COMMAND ----------

# MAGIC %md
# MAGIC #### Validacion Base de Datos

# COMMAND ----------

valida_bd(db_platinumX, 'db_platinumX')
valida_bd(db_plat_tempX, 'db_plat_tempX')
valida_bd(db_RiesgoCredEmpX, 'db_RiesgoCredEmpX')
valida_bd(db_slv_ParametricasX, 'db_slv_ParametricasX')

# COMMAND ----------

# MAGIC %md
# MAGIC #### Validacion Fecha Valida

# COMMAND ----------

valida_fecha_valida(fechaProcesoX, 'fechaProcesoX')

# COMMAND ----------

# MAGIC %md
# MAGIC #### Validacion Fecha Futura

# COMMAND ----------

valida_fecha_futura(fechaProcesoX, 'fechaProcesoX')

# COMMAND ----------

# MAGIC %md
# MAGIC #### Validacion Ruta 

# COMMAND ----------

valida_ruta(db_location_plat_tempX, 'db_location_plat_tempX')

# COMMAND ----------

# MAGIC %md
# MAGIC ### Asignacion Variables de fecha

# COMMAND ----------

ano = str(fechaProcesoX)[:4]
mes = str(fechaProcesoX)[4:][:2]
dia = str(fechaProcesoX)[6:][:2]
fechanormativo = str(ano+'-'+mes+'-'+dia)
fechacinta = str(dia+'-'+mes+'-'+ano)
anomes = str(ano+mes)
anomesdia = str(ano+mes+dia)
ano_mes_dia = str(ano+'-'+mes+'-01')

print("fecha_Formato1: " + ano)
print("fecha_Formato2: " + mes)
print("fecha_Formato3: " + dia)
print("fecha_Formato4: " + fechanormativo)
print("fecha_Formato5: " + fechacinta)
print("fecha_Formato6: " + anomes)
print("fecha_Formato7: " + anomesdia)
print("fecha_Formato8: " + ano_mes_dia)

# COMMAND ----------

# MAGIC %md
# MAGIC ## Inicio de Logica

# COMMAND ----------

# MAGIC %md
# MAGIC ## Creacion Tablas Temporales

# COMMAND ----------

# MAGIC %md
# MAGIC ###Tabla tmp_bci_modelo_ind_calif_sbif

# COMMAND ----------

drop_modelo_indiv_calif = """DROP TABLE IF EXISTS """ + db_plat_tempX + """.tmp_bci_modelo_ind_calif_sbif """

# COMMAND ----------

sql_safe(drop_modelo_indiv_calif)

# COMMAND ----------

dbutils.fs.rm(db_location_plat_tempX+"tmp_bci_modelo_ind_calif_sbif", True)

# COMMAND ----------

create_modelo_indiv_calif = """CREATE TABLE """ + db_plat_tempX + """.tmp_bci_modelo_ind_calif_sbif
(
  periodo_id INT COMMENT 'Periodo Ejecucion fomarto SSAAMM',
  rut_cliente INT COMMENT 'Rut del cliente',
  dv_cliente STRING COMMENT 'Digito verificador del cliente',
  cod_calificacion_sbif STRING COMMENT 'Clasificacion SBIF del cliente'
)
USING DELTA
PARTITIONED BY (periodo_id)
LOCATION '"""+ db_location_plat_tempX +"""tmp_bci_modelo_ind_calif_sbif' """

# COMMAND ----------

sql_safe(create_modelo_indiv_calif)

# COMMAND ----------

# MAGIC %md
# MAGIC ### Tabla bci02_basenorma_tmp_modelo_indiv

# COMMAND ----------

drop_modelo_indiv = """DROP TABLE IF EXISTS """ + db_plat_tempX + """.bci02_basenorma_tmp_modelo_indiv"""

# COMMAND ----------

sql_safe(drop_modelo_indiv)

# COMMAND ----------

dbutils.fs.rm(db_location_plat_tempX+"bci02_basenorma_tmp_modelo_indiv", True)

# COMMAND ----------

create_modelo_indiv = """CREATE TABLE """ + db_plat_tempX + """.bci02_basenorma_tmp_modelo_indiv
(
  periodo_id			  STRING,  
  rut               INTEGER,
  dv                STRING,
  nro_ope				    STRING,	
  pi_cli				    DECIMAL(30, 6),
  mont_ope			    DECIMAL(30, 6),
  provision			    DECIMAL(30, 6),
  ori_deu				    STRING,
  tip_deu				    STRING,
  tipo_garantia		  STRING,
  monto_garantia		DECIMAL(30, 6),
  calificacion		  STRING
)
USING DELTA
PARTITIONED BY (periodo_id)
LOCATION '"""+ db_location_plat_tempX +"""bci02_basenorma_tmp_modelo_indiv' """

# COMMAND ----------

sql_safe(create_modelo_indiv)

# COMMAND ----------

# MAGIC %md
# MAGIC ### Tabla bci02_basenorma_tmp_modelo_indiv_2

# COMMAND ----------

drop_modelo_indiv_2 = """DROP TABLE IF EXISTS """ + db_plat_tempX + """.bci02_basenorma_tmp_modelo_indiv_2"""

# COMMAND ----------

sql_safe(drop_modelo_indiv_2)

# COMMAND ----------

dbutils.fs.rm(db_location_plat_tempX+"bci02_basenorma_tmp_modelo_indiv_2", True)

# COMMAND ----------

create_modelo_indiv_2 = """CREATE TABLE """ + db_plat_tempX + """.bci02_basenorma_tmp_modelo_indiv_2
(
  periodo_id			  STRING,
  rut               INTEGER,
  dv                STRING,  
  nro_ope				    STRING,	
  pi_cli				    DECIMAL(30, 6),
  mont_ope			    DECIMAL(30, 6),
  provision			    DECIMAL(30, 6),
  ori_deu				    STRING,
  tip_deu				    STRING,
  tipo_garantia		  STRING,
  monto_garantia		DECIMAL(30, 6),
  calificacion		  STRING,
  pct_pi			      DECIMAL(30, 6),
  pct_pdi			      DECIMAL(30, 6),
  pct_pe			      DECIMAL(30, 6)
)
USING DELTA
PARTITIONED BY (periodo_id)
LOCATION '"""+ db_location_plat_tempX +"""bci02_basenorma_tmp_modelo_indiv_2' """

# COMMAND ----------

sql_safe(create_modelo_indiv_2)

# COMMAND ----------

# MAGIC %md
# MAGIC ### Tabla tmp_modelo_indiv

# COMMAND ----------

drop_tmp_modelo_indiv = """DROP TABLE IF EXISTS """ + db_plat_tempX + """.tmp_modelo_indiv"""

# COMMAND ----------

sql_safe(drop_tmp_modelo_indiv)

# COMMAND ----------

dbutils.fs.rm(db_location_plat_tempX+"tmp_modelo_indiv", True)

# COMMAND ----------

create_tmp_modelo_indiv = """CREATE TABLE """ + db_plat_tempX + """.tmp_modelo_indiv
(
  periodo_id			      INT,
  rut_cliente				    INT,
  dv_cliente            STRING,
  cod_num_operacion		  STRING,	
  pct_pi_cli				    DECIMAL(30, 6),
  mnt_operacion			    DECIMAL(30, 6),
  mnt_provision			    DECIMAL(30, 6),
  cod_orig_deu				  STRING,
  cod_tipo_deu				  STRING,
  cod_tipo_garantia		  STRING,
  mnt_garantia		      DECIMAL(30, 6),
  cod_calificacion		  STRING,
  pct_pi					      DECIMAL(30, 6),
  pct_pdi					      DECIMAL(30, 6),
  pct_pe					      DECIMAL(30, 7),
  fec_proceso           DATE
)
USING DELTA
PARTITIONED BY (periodo_id)
LOCATION '"""+ db_location_plat_tempX +"""tmp_modelo_indiv' """

# COMMAND ----------

sql_safe(create_tmp_modelo_indiv)

# COMMAND ----------

# MAGIC %md
# MAGIC ### Tabla bci02_basenorma_tmp_individual_repetidos

# COMMAND ----------

drop_individual_repetidos = """DROP TABLE IF EXISTS """ + db_plat_tempX + """.bci02_basenorma_tmp_individual_repetidos"""

# COMMAND ----------

sql_safe(drop_individual_repetidos)

# COMMAND ----------

dbutils.fs.rm(db_location_plat_tempX+"bci02_basenorma_tmp_individual_repetidos", True)

# COMMAND ----------

create_individual_repetidos = """CREATE TABLE """ + db_plat_tempX + """.bci02_basenorma_tmp_individual_repetidos
(
  cod_num_operacion			  STRING
)
USING DELTA
LOCATION '"""+ db_location_plat_tempX +"""bci02_basenorma_tmp_individual_repetidos' """

# COMMAND ----------

sql_safe(create_individual_repetidos)

# COMMAND ----------

# MAGIC %md
# MAGIC ### Tabla tmp_individual_repetidos

# COMMAND ----------

drop_tmp_individual_repetidos = """DROP TABLE IF EXISTS """ + db_plat_tempX + """.tmp_individual_repetidos"""

# COMMAND ----------

sql_safe(drop_tmp_individual_repetidos)

# COMMAND ----------

dbutils.fs.rm(db_location_plat_tempX+"tmp_individual_repetidos", True)

# COMMAND ----------

create_tmp_individual_repetidos = """CREATE TABLE """ + db_plat_tempX + """.tmp_individual_repetidos
(
  periodo_id              INT,
  cod_num_operacion			  STRING,
  fec_proceso             DATE
)
USING DELTA
PARTITIONED BY (periodo_id)
LOCATION '"""+ db_location_plat_tempX +"""tmp_individual_repetidos' """

# COMMAND ----------

sql_safe(create_tmp_individual_repetidos)

# COMMAND ----------

# MAGIC %md
# MAGIC ## Desarrollo Logica

# COMMAND ----------

ult_dia_mes = ultimo_dia_habil(db_slv_ParametricasX, anomes)

ult_dia = ult_dia_mes.toPandas()
fec_proceso = ult_dia.iloc[0]["periodo_id"]

ano_proceso = str(fec_proceso)[:4]
mes_proceso = str(fec_proceso)[4:][:2]
dia_proceso = str(fec_proceso)[6:][:2]
date_proceso = str(ano_proceso+'-'+mes_proceso+'-'+dia_proceso)

print("fecha_Formato9: " + date_proceso)

# COMMAND ----------

# MAGIC %md
# MAGIC ###Paso 1: Obtencion Calificaciones Clientes

# COMMAND ----------

# MAGIC %md
# MAGIC Se genera una Vista con los datos de Calificaciones SBIF de clientes bci (no incluye filiales) para cartera individual comercial.
# MAGIC
# MAGIC **Tabla de salida**: tmp_bci_modelo_ind_calif_sbif (vista temporal)
# MAGIC
# MAGIC **variables de salida:**
# MAGIC - **periodo_id**: Periodo Ejecucion fomarto SSAAMM
# MAGIC - **rut_cliente**: Rut del cliente
# MAGIC - **dv_cliente**: Digito verificador del cliente
# MAGIC - **cod_calificacion_sbif**: Clasificacion SBIF del cliente

# COMMAND ----------

qry_ins_calificaciones = """ INSERT INTO """+db_plat_tempX+""".tmp_bci_modelo_ind_calif_sbif 
select distinct fec_periodo as periodo_id
    ,Rut                    as rut_cliente
    ,DV                     as dv_cliente
    ,Clasificacion_SBIF     as cod_calificacion_sbif 
from """+db_RiesgoCredEmpX+""".prov_emp_per
where fec_periodo = '"""+anomes+"""'
"""

# COMMAND ----------

sql_safe(qry_ins_calificaciones)

# COMMAND ----------

# MAGIC %md
# MAGIC ### Paso 2: Obtencion Univ Modelo Individual 1

# COMMAND ----------

# MAGIC %md
# MAGIC Se crea una vista para generar la informacion del universo modelo individual desde mitcalcprov y tabcli_calificacion
# MAGIC
# MAGIC **Tabla de salida**: bci02_basenorma_tmp_modelo_indiv (vista temporal)
# MAGIC
# MAGIC **variables de salida:**
# MAGIC - **periodo_id**: Fecha de cierre AAAA-MM-DD
# MAGIC - **cliente_id**: Rut del cliente
# MAGIC - **rut_dv**: Rut con digito verificador del cliente
# MAGIC - **rut**: Rut del cliente
# MAGIC - **dv**: digito verificador del cliente
# MAGIC - **colocacion_id**: Codigo operacion
# MAGIC - **nro_ope**: Codigo operacion
# MAGIC - **pi_cli**: Porcentaje probabilidad de incumplimiento del cliente
# MAGIC - **mont_ope**: Monto de la operacion
# MAGIC - **provision**: Monto provisional
# MAGIC - **ori_deu**: Entidad de origen deudor
# MAGIC - **tip_deu**: Tipo de deudor
# MAGIC - **tipo_garantia**: Tiene o no garantia
# MAGIC - **monto_garantia**: Monto de la garantia
# MAGIC - **calificacion**: Codigo clasificacion					

# COMMAND ----------

vista_modelo_indiv = """ insert into """ + db_plat_tempX + """.bci02_basenorma_tmp_modelo_indiv
select """+anomes+"""                 as periodo_id    
    ,a.cli_rut                        as rut
    ,a.dv                             as dv
    ,case when a.ori_deu = 'LEA' then ('H'||  string(LPAD(TRIM(cast(a.nro_ope as char(11))),11,'0' )) ) else trim(a.nro_ope) END as nro_ope
    ,a.pi_cli
    ,a.mto_ope                      as mont_ope
    ,a.provision
    ,a.ori_deu
    ,a.tip_deu
    ,case when a.gar_gral = 0 then (case when a.gar_esp = 0 then 'No_Tiene' else 'E' end)
	      else 'G'  end                            as tipo_garantia
    ,case  when a.gar_gral = 0 then (case when a.gar_esp = 0 then 0 else a.gar_esp end)
        else a.gar_gral  end	                          as monto_garantia
    ,b.cod_calificacion_sbif        as calificacion
from """ + db_RiesgoCredEmpX + """.mitcalcprov a
left join """+db_plat_tempX+""".tmp_bci_modelo_ind_calif_sbif  b
    on a.cli_rut = b.rut_cliente
    and concat(substring(a.fecha_proceso, 7, 4), substring(a.fecha_proceso, 4, 2) ) = b.periodo_id
  where concat(substring(a.fecha_proceso, 7, 4), substring(a.fecha_proceso, 4, 2)) = '""" + anomes + """'
"""

# COMMAND ----------

sql_safe(vista_modelo_indiv)

# COMMAND ----------

import pandas as pd
import sys
import os

def Consultas(query, tabla):
  jdbcHostname = "10.241.16.4"
  jdbcPort = 1433
  #jdbcUrl = "jdbc:sqlserver://{0}:{1};".format(jdbcHostname, jdbcPort)
  jdbcUrl = "jdbc:sqlserver://10.241.16.4:1433;database=SBX_RI_Capital;encrypt=true;trustServerCertificate=true;"
  connectionProperties = {
    "user" :  "Basilea",
    "password" : "Basilea2",
    "driver" : "com.microsoft.sqlserver.jdbc.SQLServerDriver"
  }
  Consulta = '(' + query + ') as A'
  print("query --> " + Consulta + "")
  Resultado = spark.read.jdbc(url=jdbcUrl, table=Consulta, properties=connectionProperties)
  DfResultados = Resultado
  DfResultados.registerTempTable('TMP_'+tabla)
  print(Resultado)

  return(Resultado)

# COMMAND ----------

query1 = """ select * from BCI_RI_MODELO..TAB_MITCALCPROV_INDV where Periodo_Id = '2026-02-01' """
tabla = 'TAB_MITCALCPROV_INDV'

# COMMAND ----------

Consultas(query1,tabla)

# COMMAND ----------

# MAGIC %md
# MAGIC ### Paso 3: Obtencion Univ Modelo Individual 2

# COMMAND ----------

# MAGIC %md
# MAGIC Se crea una vista para generar la informacion del universo modelo individual 2 desde bci02_basenorma_tmp_modelo_indiv y tabmae_tablapi_sbif
# MAGIC
# MAGIC **Tabla de salida**: bci02_basenorma_tmp_modelo_indiv_2 (vista temporal)
# MAGIC
# MAGIC **variables de salida:**
# MAGIC - **periodo_id**: Fecha de cierre AAAAMM
# MAGIC - **rut**: Rut del cliente
# MAGIC - **dv**: digito verificador del cliente
# MAGIC - **nro_ope**: Codigo operacion
# MAGIC - **pi_cli**: Porcentaje probabilidad de incumplimiento del cliente
# MAGIC - **mont_ope**: Monto de la operacion
# MAGIC - **provision**: Monto provisional
# MAGIC - **ori_deu**: Entidad de origen deudor
# MAGIC - **tip_deu**: Tipo de deudor
# MAGIC - **tipo_garantia**: Tiene o no garantia
# MAGIC - **monto_garantia**: Monto de la garantia
# MAGIC - **calificacion**: Codigo clasificacion 
# MAGIC - **pi**: Porcentaje de la probabilidad incumplimiento
# MAGIC - **pdi**: Porcentaje de perdida dada por el incumplimiento
# MAGIC - **pe**: Porcentaje de perdida esperada						

# COMMAND ----------

vista_modelo_indiv_2 = """ insert into """ + db_plat_tempX + """.bci02_basenorma_tmp_modelo_indiv_2
select  a.periodo_id
    ,a.rut
    ,a.dv          
    ,a.nro_ope
    ,a.pi_cli
    ,a.mont_ope
    ,a.provision
    ,a.ori_deu
    ,a.tip_deu
    ,a.tipo_garantia
    ,a.monto_garantia
    ,a.calificacion
    ,b.pct_pi
    ,b.pct_pdi
    ,b.pct_pe
from """ + db_plat_tempX + """.bci02_basenorma_tmp_modelo_indiv a
left join """ + db_platinumX + """.tabmae_tablapi_sbif b
    on trim(a.calificacion) = trim(b.cod_clasificacion)
""" 

# COMMAND ----------

sql_safe(vista_modelo_indiv_2)

# COMMAND ----------

# MAGIC %md
# MAGIC ### Paso 4: Eliminacion Datos Modelo Individual para Fecha de Proceso 

# COMMAND ----------

# MAGIC %md
# MAGIC Se elimina los datos de la tabla prv_fac_ft_probes_pi correspondiente a la fecha de proceso que se va a insertar posteriormente
# MAGIC
# MAGIC **Tabla de salida**: prv_fac_ft_probes_pi (tabla de salida)

# COMMAND ----------

delete_tmp_modelo_indiv = """delete from """ + db_plat_tempX + """.tmp_modelo_indiv where periodo_id = '""" + anomes + """' """

# COMMAND ----------

sql_safe(delete_tmp_modelo_indiv)

# COMMAND ----------

# MAGIC %md
# MAGIC ### Paso 5: Insercion Datos Tabla tmp_modelo_indiv

# COMMAND ----------

# MAGIC %md
# MAGIC Se prepara e inserta los datos a la tabla tmp_modelo_indiv desde la vista bci02_basenorma_tmp_modelo_indiv
# MAGIC
# MAGIC **Tabla de salida**: tmp_modelo_indiv (tabla de salida)
# MAGIC
# MAGIC **variables de salida:**
# MAGIC - **periodo_id**: Fecha de cierre AAAAMM
# MAGIC - **rut_cliente**: Rut del cliente
# MAGIC - **dv_cliente**: digito verificador del cliente
# MAGIC - **cod_num_operacion**: Codigo operacion
# MAGIC - **pct_pi_cli**: Porcentaje probabilidad de incumplimiento del cliente
# MAGIC - **mnt_operacion**: Monto de la operacion
# MAGIC - **mnt_provision**: Monto provisional
# MAGIC - **cod_orig_deu**: Entidad de origen deudor
# MAGIC - **cod_tipo_deu**: Tipo de deudor
# MAGIC - **cod_tipo_garantia**: Tiene o no garantia
# MAGIC - **mnt_garantia**: Monto de la garantia
# MAGIC - **cod_calificacion**: Codigo clasificacion 
# MAGIC - **pct_pi**: Porcentaje de la probabilidad incumplimiento
# MAGIC - **pct_pdi**: Porcentaje de perdida dada por el incumplimiento
# MAGIC - **pct_pe**: Porcentaje de perdida esperada	
# MAGIC - **fec_proceso**: Fecha de cierre

# COMMAND ----------

insert_tmp_modelo_indiv = """ insert into """ + db_plat_tempX + """.tmp_modelo_indiv
  select  """+anomes+"""                  as periodo_id
      ,rut                                as rut_cliente
      ,dv                                 as dv_cliente
      ,nro_ope                            as cod_num_operacion
      ,pi_cli                             as pct_pi_cli
      ,mont_ope                           as mnt_operacion
      ,provision                          as mnt_provision
      ,ori_deu                            as cod_orig_deu		
      ,tip_deu                            as cod_tipo_deu
      ,tipo_garantia                      as cod_tipo_garantia
      ,monto_garantia                     as mnt_garantia
      ,calificacion                       as cod_calificacion
      ,pct_pi                             as pct_pi	
      ,pct_pdi                            as pct_pdi
      ,pct_pe                             as pct_pe		     
      ,'""" + date_proceso + """'         as fec_proceso
  from """ + db_plat_tempX + """.bci02_basenorma_tmp_modelo_indiv_2

"""

# COMMAND ----------

sql_safe(insert_tmp_modelo_indiv)

# COMMAND ----------

# MAGIC %md
# MAGIC ### Paso 6: Obtencion Operaciones Individuales Repetidos

# COMMAND ----------

# MAGIC %md
# MAGIC Se crea una vista para generar la informacion del universo modelo individual desde tmp_colcomer y bci02_basenorma_tmp_modelo_indiv
# MAGIC
# MAGIC **Tabla de salida**: bci02_basenorma_tmp_individual_repetidos (vista temporal)
# MAGIC
# MAGIC **variables de salida:**
# MAGIC - **cod_num_operacion**: Código Número Operación		

# COMMAND ----------

vista_individual_repetidos = """ insert into """ + db_plat_tempX + """.bci02_basenorma_tmp_individual_repetidos
select a.cod_num_operacion
from """ + db_plat_tempX + """.tmp_colcomer a
inner join """ + db_plat_tempX + """.bci02_basenorma_tmp_modelo_indiv b
  on a.periodo_id = b.periodo_id
  and ((trim(a.cod_num_operacion) = trim(b.nro_ope)
      and trim(b.tip_deu) = 'NCONT'
      and b.provision != 0)
      or  (trim(a.cod_num_operacion) = trim(b.nro_ope)
        and trim(b.tip_deu) = 'NCONT'
        and b.provision = 0
        and (substring(trim(a.cod_num_operacion), 1,1) in ('C', 'V')))
      or (trim(a.cod_num_operacion) = trim(b.nro_ope)
        and trim(b.tip_deu) = 'NCONT'
        and b.provision = 0
        and substring(trim(a.cod_tipo_ope), 1,3) = 'TDC'))      
  where a.periodo_id  = """+anomes+"""
  group by a.cod_num_operacion
  having count(*) > 1
"""

# COMMAND ----------

sql_safe(vista_individual_repetidos)

# COMMAND ----------

# MAGIC %md
# MAGIC ### Paso 7: Eliminacion Datos Modelo Individual Repetidos

# COMMAND ----------

# MAGIC %md
# MAGIC Se elimina los datos de la tabla tmp_individual_repetidos correspondiente a la fecha de proceso que se va a insertar posteriormente
# MAGIC
# MAGIC **Tabla de salida**: tmp_individual_repetidos (tabla de salida)

# COMMAND ----------

delete_tmp_modelo_indiv = """delete from """ + db_plat_tempX + """.tmp_individual_repetidos where periodo_id = """ + anomes + """ """

# COMMAND ----------

sql_safe(delete_tmp_modelo_indiv)

# COMMAND ----------

# MAGIC %md
# MAGIC ### Paso 8: Insercion Datos Tabla tmp_individual_repetidos

# COMMAND ----------

# MAGIC %md
# MAGIC Se prepara e inserta los datos a la tabla tmp_individual_repetidos desde la vista bci02_basenorma_tmp_individual_repetidos
# MAGIC
# MAGIC **Tabla de salida**: tmp_individual_repetidos (tabla de salida)
# MAGIC
# MAGIC **variables de salida:**
# MAGIC - **periodo_id**: Fecha de cierre AAAAMM
# MAGIC - **cod_num_operacion**: Código Número Operación		
# MAGIC - **fec_proceso**: Fecha de cierre

# COMMAND ----------

insert_tmp_individual_repetidos = """ insert into """ + db_plat_tempX + """.tmp_individual_repetidos
  select  '""" + anomes + """'              as periodo_id,
          cod_num_operacion,			     
          '""" + date_proceso + """'        as fec_proceso
  from """ + db_plat_tempX + """.bci02_basenorma_tmp_individual_repetidos
"""

# COMMAND ----------

sql_safe(insert_tmp_individual_repetidos)

# COMMAND ----------

# MAGIC %md
# MAGIC ## Conteo de registros insertados en tabla de salida

# COMMAND ----------

registros = spark.sql(""" select count(1) as cant_registros from """+db_plat_tempX+""".tmp_modelo_indiv  where periodo_id = """+anomes+""" """)

reg_insertados = registros.toPandas()
cantidad = reg_insertados.iloc[0]["cant_registros"]

print("Se Insertaron " + str(cantidad) +" Registros en la tabla temporal tmp_modelo_indiv ")

# COMMAND ----------

registros = spark.sql(""" select count(1) as cant_registros from """+db_plat_tempX+""".tmp_individual_repetidos  where periodo_id = """+anomes+""" """)

reg_insertados = registros.toPandas()
cantidad = reg_insertados.iloc[0]["cant_registros"]

print("Se Insertaron " + str(cantidad) +" Registros en la tabla temporal tmp_individual_repetidos ")

# COMMAND ----------

# MAGIC %md
# MAGIC # Mensaje Final

# COMMAND ----------

dbutils.notebook.exit("{\"coderror\":\"0\", \"msgerror\":\"Notebook termina ejecucion satisfactoriamente\"}")